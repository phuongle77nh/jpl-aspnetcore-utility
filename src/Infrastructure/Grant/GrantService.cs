using Dapper;
using Mapster;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using JPL.NetCoreUtility.Application.Common.Persistence;
using JPL.NetCoreUtility.Application.Grant;
using JPL.NetCoreUtility.Application.UserScope;
using JPL.NetCoreUtility.Domain.Grant;
using JPL.NetCoreUtility.Infrastructure.Common.Extensions;
using JPL.NetCoreUtility.Infrastructure.Persistence.Context;
using JPL.NetCoreUtility.Shared.Multitenancy;
using StackExchange.Redis;
using System.Linq;
using System.Text;

namespace JPL.NetCoreUtility.Infrastructure.Grant;

#region Models
public class GetRolesResponse
{
    public List<RoleDto> Roles { get; set; }
}

public class RoleDto
{
    public string Id { get; set; }
    public string TenantId { get; set; }
    public string RoleName { get; set; }

    public Guid GetId()
    {
        return new Guid(Id);
    }
}

public class SecurityAttributeDto
{
    public Guid AttributeId { get; set; }
    public string AttributeQuery { get; set; }
    public int Permission { get; set; }
}

public class SecurityRoleDto
{
    public string RoleId { get; set; }
    public int Permission { get; set; }
}

public class TargetResultDto
{
    public Guid SourceId { get; set; }
    public Guid EntityId { get; set; }
    public int SourceType { get; set; }
    public int Permission { get; set; }
}

public class MyselfQueryResponse
{
    public object EntityId { get; set; }
    public object UserId { get; set; }
}

public class EntityScopeDto
{
    public Guid EntityId { get; set; }
    public Guid ScopeId { get; set; }
}
#endregion

public class GrantService : IGrantService
{
    private const string RsSecurityDb = "jpl_security";
    private readonly ApplicationDbContext _context;
    private readonly IDapperRepository _repository;

    public GrantService(
        ApplicationDbContext context,
        IDapperRepository repository)
    {
        _context = context;
        _repository = repository;
    }

    public async Task<List<ServiceDto>> ExportService(string serviceName)
    {
        var services = await _context.Services.Where(s => s.ServiceName == serviceName).ProjectToType<ServiceDto>().ToListAsync();

        var roles = await _repository.ExecStoredProcAsync<RoleDto>(SprocConstants.GetRoles);
        foreach (var service in services)
        {
            foreach (var serviceEntity in service.ServiceEntities)
            {
                foreach (var policy in serviceEntity.Policies)
                {
                    foreach (var policyLink in policy.PolicyLinks)
                    {
                        if (policyLink.EntityType == EntityTypeEnum.Role)
                        {
                            var role = roles.FirstOrDefault(x => x.GetId() == policyLink.EntityId && policyLink.EntityType == EntityTypeEnum.Role);
                            if (role != null)
                            {
                                policyLink.EntityValue = role.RoleName;
                                policyLink.TenantId = role.TenantId;
                            }
                        }
                        else if (policyLink.EntityType == EntityTypeEnum.Attribute)
                        {
                            var attribute = serviceEntity.SecurityAttributes.FirstOrDefault(x => x.Id == policyLink.EntityId && policyLink.EntityType == EntityTypeEnum.Attribute);
                            if (attribute != null)
                            {
                                policyLink.AttributeName = attribute.AttributeName;
                            }
                        }
                    }
                }
            }
        }

        return services;
    }

    public async Task<string> ImportService(List<ServiceDto> serviceDtos)
    {
        var roles = await _repository.ExecStoredProcAsync<RoleDto>("jpl_authentication.[Identity].[GetRoles]");

        foreach (var serviceDto in serviceDtos)
        {
            var service = _context.Services
                .Include(x => x.ServiceEntities).ThenInclude(x => x.Policies)
                .Include(x => x.ServiceEntities).ThenInclude(x => x.SecurityAttributes)
                .Include(x => x.ServiceEntities).ThenInclude(x => x.Policies).ThenInclude(x => x.PolicyLinks)
                .FirstOrDefault(x => x.ServiceName == serviceDto.ServiceName);
            var isNew = false;
            if (service != null)
            {
                service.Database = serviceDto.Database;
            }
            else
            {
                isNew = true;

                service = new Service
                {

                    ServiceName = serviceDto.ServiceName,
                    Database = serviceDto.Database
                };
                _context.Services.Add(service);
            }

            foreach (var serviceEntityDto in serviceDto.ServiceEntities)
            {
                var serviceEntity = service.ServiceEntities?.FirstOrDefault(x =>
                x.TableName == serviceEntityDto.TableName
                && x.TableSchema == serviceEntityDto.TableSchema
                && x.PrimaryColumnName == serviceEntityDto.PrimaryColumnName);

                if (serviceEntity != null)
                {
                    serviceEntity.MyselfQuery = serviceEntityDto.MyselfQuery;
                }
                else
                {
                    serviceEntity = new ServiceEntity
                    {
                        ServiceId = service.Id,
                        TableName = serviceEntityDto.TableName,
                        TableSchema = serviceEntityDto.TableSchema,
                        PrimaryColumnName = serviceEntityDto.PrimaryColumnName,
                        MyselfQuery = serviceEntityDto.MyselfQuery
                    };

                    _context.Add(serviceEntity);
                }

                if (serviceEntityDto.SecurityAttributes != null && serviceEntityDto.SecurityAttributes.Any())
                {
                    foreach (var securityAttributeDto in serviceEntityDto.SecurityAttributes)
                    {
                        var securityAttribute = serviceEntity.SecurityAttributes?.FirstOrDefault(x => x.AttributeName == securityAttributeDto.AttributeName);

                        if (securityAttribute != null)
                        {
                            securityAttribute.AttributeQuery = securityAttributeDto.AttributeQuery;
                        }
                        else
                        {
                            securityAttribute = new SecurityAttribute
                            {
                                ServiceEntityId = serviceEntity.Id,
                                AttributeName = securityAttributeDto.AttributeName,
                                AttributeQuery = securityAttributeDto.AttributeQuery
                            };

                            _context.Add(securityAttribute);
                        }
                    }
                }

                foreach (var policyDto in serviceEntityDto.Policies)
                {
                    var policy = serviceEntity.Policies?.FirstOrDefault(x => x.PolicyName == policyDto.PolicyName);

                    if (policy == null)
                    {
                        policy = new Policy
                        {
                            ServiceEntityId = serviceEntity.Id,
                            PolicyName = policyDto.PolicyName,
                            Permission = policyDto.Permission
                        };

                        _context.Add(policy);
                    }
                    else
                    {
                        policy.Permission = policyDto.Permission;
                        policy.PolicyLinks.Clear();
                    }

                    foreach (var policyLinkDto in policyDto.PolicyLinks)
                    {
                        if (policyLinkDto.EntityType == EntityTypeEnum.Role)
                        {
                            string entityValue = policyLinkDto.EntityValue;
                            string tenantId = policyLinkDto.TenantId;
                            var role = roles.FirstOrDefault(x => x.RoleName == entityValue && x.TenantId == tenantId);

                            if (role != null)
                            {
                                var policyLinkRole = policy.PolicyLinks?.FirstOrDefault(x => x.EntityType == EntityTypeEnum.Role && x.EntityId.ToString() == role.Id);

                                if (policyLinkRole == null)
                                {
                                    policyLinkRole = new PolicyLink
                                    {
                                        PolicyId = policy.Id,
                                        EntityId = new Guid(role.Id),
                                        EntityType = EntityTypeEnum.Role,
                                        EntityFormat = policyLinkDto.EntityFormat
                                    };

                                    _context.Add(policyLinkRole);
                                }
                            }
                        }
                        else if (policyLinkDto.EntityType == EntityTypeEnum.Attribute)
                        {
                            var securityAttribute = serviceEntity.SecurityAttributes.FirstOrDefault(x => x.AttributeName == policyLinkDto.AttributeName);
                            var policyLinkAttribute = policy.PolicyLinks?.FirstOrDefault(x => x.EntityType == EntityTypeEnum.Attribute && x.EntityId == securityAttribute.Id);
                            if (policyLinkAttribute == null)
                            {
                                policyLinkAttribute = new PolicyLink
                                {
                                    PolicyId = policy.Id,
                                    EntityId = securityAttribute.Id,
                                    EntityType = EntityTypeEnum.Attribute,
                                    EntityFormat = policyLinkDto.EntityFormat
                                };

                                _context.Add(policyLinkAttribute);
                            }
                        }
                    }
                }
            }

            if (!isNew)
            {
                _context.Services.Update(service);
            }
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return ex.GetAllMessages();
        }

        return "Done";
    }

    public async Task<string> CreateService(string serviceId, string serviceName, string databaseName)
    {
        var serviceGuid = new Guid(serviceId);
        var service = _context.Services.Find(serviceGuid);
        if (service == null)
        {
            service = new Domain.Grant.Service
            {
                Id = serviceGuid,
                ServiceName = serviceName,
                Database = databaseName
            };
            _context.Services.Add(service);
        }
        else
        {
            service.ServiceName = serviceName;
            service.Database = databaseName;
            _context.Update(service);
        }

        int result = await _context.SaveChangesAsync();
        return result.ToString();
    }

    public async Task<string> CreateServiceEntity(string serviceEntityId, string serviceId, string tableName, string tableSchema, string primaryColumnName)
    {
        var serviceGuid = new Guid(serviceId);
        var serviceEntityGuid = new Guid(serviceEntityId);
        var serviceEntity = _context.ServiceEntities.Find(serviceEntityGuid);
        if (serviceEntity == null)
        {
            serviceEntity = new ServiceEntity
            {
                Id = serviceEntityGuid,
                TableName = tableName,
                TableSchema = tableSchema,
                PrimaryColumnName = primaryColumnName,
                ServiceId = serviceGuid
            };
            _context.ServiceEntities.Add(serviceEntity);
        }
        else
        {
            serviceEntity.TableName = tableName;
            serviceEntity.TableSchema = tableSchema;
            serviceEntity.PrimaryColumnName = primaryColumnName;
            serviceEntity.ServiceId = serviceGuid;
            _context.Update(serviceEntity);
        }

        int result = await _context.SaveChangesAsync();
        return result.ToString();
    }

    public async Task<string> DeleteService(string serviceName)
    {
        var service = await _context.Services.FirstOrDefaultAsync(x => x.ServiceName == serviceName);
        if (service == null) return "Not found.";


        _context.Services.Remove(service);
        await _context.SaveChangesAsync();
        return "Deleted";
    }

    public async Task<List<string>> GeneratePermission(string serviceName)
    {
        var service = _context.Services
            .Include(x => x.ServiceEntities)
            .FirstOrDefault(x => x.ServiceName == serviceName);
        if (service == null) return new List<string> { "service not found"};
        List<string> results = new List<string>();
        foreach (var serviceEntity in service.ServiceEntities)
        {
            try
            {
                await _context.Connection.ExecuteAsync(
                                "[Grant].[GeneratePermission]",
                                new
                                {
                                    InputServiceEntityId = serviceEntity.Id
                                },
                                commandType: System.Data.CommandType.StoredProcedure);
                results.Add($"Generated success : {serviceEntity.TableSchema}.{serviceEntity.TableName}");
            }
            catch (Exception ex)
            {
                results.Add($"Failed : {serviceEntity.TableSchema}.{serviceEntity.TableName}");
            }
        }

        return results;
    }

    public async Task<List<string>> GeneratePermissionForEntity(string serviceName, string tableSchema, string tableName, string userEmail = "", string tenantId = "restaff")
    {
        var service = _context.Services.AsNoTracking()
            .Include(x => x.ServiceEntities).ThenInclude(x => x.Policies)
            .Include(x => x.ServiceEntities).ThenInclude(x => x.Policies).ThenInclude(x => x.PolicyLinks)
            .FirstOrDefault(x => x.ServiceName.ToLower() == serviceName.ToLower());
        if (service == null) return new List<string> { "service not found" };

        var serviceEntity = service.ServiceEntities.FirstOrDefault(x => x.TableName.ToLower() == tableName.ToLower() && x.TableSchema.ToLower() == tableSchema.ToLower());
        if (serviceEntity == null) return new List<string> { "serviceEntity not found" };

        List<string> results = new List<string>();
        var users = await _repository.ExecStoredProcAsync<UserInfoDto>(SprocConstants.GetUsers, new { TenantId = tenantId });
        var user = users.FirstOrDefault(x => x.WorkEmail == userEmail);
        Guid userId = Guid.Empty;
        if (user != null) userId = user.UserId;

        try
        {
            #region Generate Table store permission
            var targetTableName = $"{service.Database}.[{serviceEntity.TableSchema}].[{serviceEntity.TableName}]";
            results.Add($"targetTableName: {targetTableName}");

            var generatedTableName = $"{RsSecurityDb}.[Generated].{service.Database}_{serviceEntity.TableSchema.ToLower()}_{serviceEntity.TableName.ToLower()}";
            var sqlCreateTableSb = new StringBuilder();
            sqlCreateTableSb.Append($"IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'{generatedTableName}') AND type in (N'U'))");
            sqlCreateTableSb.AppendLine("BEGIN");
            sqlCreateTableSb.AppendLine($"CREATE TABLE {generatedTableName}(SourceId UNIQUEIDENTIFIER, EntityId UNIQUEIDENTIFIER, SourceType int, Permission int)");
            sqlCreateTableSb.AppendLine("END");

            try
            {
                await _context.Connection.ExecuteAsync(
                                sqlCreateTableSb.ToString(),
                                commandType: System.Data.CommandType.Text);
                results.Add($"Success execute sql create table {generatedTableName}");

            }
            catch (Exception ex)
            {
                results.Add($"Fail execute sql create table {generatedTableName}. SqlQuery: {sqlCreateTableSb}");
            }
            #endregion

            #region Attribute Security
            var sqlTargetFromAttributeSb = new StringBuilder();
            var targetFromAttributeData = new List<TargetResultDto>();

            var attrPermData = serviceEntity.Policies
                .SelectMany(x => x.PolicyLinks)
                .Where(x => x.EntityType == EntityTypeEnum.Attribute)
                .Select(x => new SecurityAttributeDto
                {
                    AttributeId = x.EntityId,
                    Permission = x.Policy.Permission
                })
                .ToList();

            var attributeIds = attrPermData.Select(x => x.AttributeId).ToList();

            var attrQueryData = _context.Attributes.AsNoTracking().Where(x => attributeIds.Contains(x.Id))
                .Select(x => new SecurityAttributeDto
                {
                    AttributeId = x.Id,
                    AttributeQuery = x.AttributeQuery
                }).ToList();

            attrQueryData.ForEach(x =>
            {
                x.Permission = attrPermData.FirstOrDefault(y => y.AttributeId == x.AttributeId)?.Permission ?? 0;
            });

            for (int i = 0; i < attrQueryData.Count; i++)
            {
                sqlTargetFromAttributeSb.AppendLine($"SELECT attributeQuery.UserId AS SourceId, attributeQuery.EntityId, policyQuery.Permission, policyQuery.SourceType FROM ({attrQueryData[i].AttributeQuery}) AS attributeQuery OUTER APPLY (SELECT {attrQueryData[i].Permission} AS Permission, {(int)EntityTypeEnum.Attribute} AS SourceType) AS policyQuery");
                if (i < attrQueryData.Count - 1)
                {
                    sqlTargetFromAttributeSb.AppendLine("UNION");
                }
            }

            try
            {
                if (sqlTargetFromAttributeSb.Length != 0)
                {
                    var targetFromAttributeDataResponse = await _context.Connection.QueryAsync<TargetResultDto>(
                                    sqlTargetFromAttributeSb.ToString(),
                                    commandType: System.Data.CommandType.Text);
                    targetFromAttributeData = targetFromAttributeDataResponse.ToList();
                    results.Add($"Success execute sql generate from attribute");
                }
            }
            catch (Exception ex)
            {
                results.Add($"Fail execute sql generate from attribute. SqlQuery: {sqlTargetFromAttributeSb}");
            }
            #endregion

            #region Scope Security
            var sqlTargetFromScopeSb = new StringBuilder();
            var targetFromScopeData = new List<TargetResultDto>();

            try
            {
                var userScopes = await _repository.ExecStoredProcAsync<UserScopeDto>(SprocConstants.GetUserScopes, new { TenantId = tenantId });

                var roleSecurityItems = serviceEntity.Policies
                    .SelectMany(x => x.PolicyLinks)
                    .Where(x => x.EntityType == EntityTypeEnum.Role)
                    .Select(x => new SecurityRoleDto
                    {
                        RoleId = x.EntityId.ToString(),
                        Permission = x.Policy.Permission
                    }).ToList();

                var myselfData = await _context.Connection.QueryAsync<MyselfQueryResponse>(
                                        serviceEntity.MyselfQuery,
                                        commandType: System.Data.CommandType.Text);

                var entityScopes = new List<EntityScopeDto>();
                foreach (var myself in myselfData)
                {
                    var myselfScopes = userScopes.Where(x => x.UserId == myself.UserId);
                    foreach (var item in myselfScopes)
                    {
                        entityScopes.Add(new EntityScopeDto
                        {
                            EntityId = new Guid(myself.EntityId.ToString()),
                            ScopeId = item.ScopeId
                        });
                    }
                }

                foreach (var roleSecurityItem in roleSecurityItems)
                {
                    var usersInSecurityRole = userScopes.Where(x => roleSecurityItem.RoleId == x.RoleId).ToList();
                    var targetData = usersInSecurityRole.Join(entityScopes, user => user.ScopeId, entity => entity.ScopeId, (user, entity) =>
                    new TargetResultDto
                    {
                        SourceId = new Guid(user.UserId),
                        EntityId = entity.EntityId,
                        SourceType = (int)EntityTypeEnum.Role,
                        Permission = roleSecurityItem.Permission
                    });

                    targetFromScopeData.AddRange(targetData);
                }

                results.Add($"Success generate target from scope");
            }
            catch (Exception ex)
            {
                results.Add($"Fail generate target from scope. Exception: {ex}");

            }
            #endregion
            var debugId = new Guid();
            var debugAttr = targetFromAttributeData.Where(x => x.EntityId == debugId).ToList();
            if (debugAttr.Any())
            {
                foreach (var item in debugAttr)
                {
                    results.Add($"targetFromAttributeData debugId: {item.SourceId}");
                }
            }

            var debugScope = targetFromScopeData.Where(x => x.EntityId == debugId).ToList();
            if (debugScope.Any())
            {
                foreach (var item in debugScope)
                {
                    results.Add($"targetFromScopeData debugId: {item.SourceId}");

                }
            }

            if (userId != Guid.Empty)
            {
                var targetUserAttr = targetFromAttributeData.Where(x => x.SourceId == userId).ToList();
                if (targetUserAttr.Any())
                {
                    foreach (var item in targetUserAttr)
                    {
                        results.Add($"targetFromAttributeData userId: {item.EntityId}");
                    }
                }

                var targetUserScope = targetFromScopeData.Where(x => x.SourceId == userId).ToList();
                if (targetUserScope.Any())
                {
                    foreach (var item in targetUserScope)
                    {
                        results.Add($"targetFromScopeData userId: {item.EntityId}");
                    }
                }
            }

            var finalTargetData = targetFromAttributeData.Union(targetFromScopeData).ToList();

            #region Merge Security
            var sqlMergeSecuritySb = new StringBuilder();

            sqlMergeSecuritySb.AppendLine($"IF OBJECT_ID('tempdb..#tempTableGeneratedPermSecured') IS NOT NULL DROP TABLE #tempTableGeneratedPermSecured");
            sqlMergeSecuritySb.AppendLine($"CREATE TABLE #tempTableGeneratedPermSecured( SourceId UNIQUEIDENTIFIER, EntityId UNIQUEIDENTIFIER, SourceType INT ,Permission INT)");
            foreach ( var item in finalTargetData)
            {
                sqlMergeSecuritySb.AppendLine($"INSERT INTO #tempTableGeneratedPermSecured( SourceId, EntityId, SourceType , Permission) VALUES('{item.SourceId}','{item.EntityId}',{item.SourceType},{item.Permission})");
            }

            sqlMergeSecuritySb.AppendLine($"MERGE INTO {generatedTableName} AS tgt");
            sqlMergeSecuritySb.AppendLine($"USING #tempTableGeneratedPermSecured AS src");
            sqlMergeSecuritySb.AppendLine("ON (tgt.[SourceId]=src.[SourceId] AND tgt.[EntityId]=src.[EntityId]) AND tgt.SourceType = src.SourceType AND tgt.[Permission]=src.[Permission]");
            sqlMergeSecuritySb.AppendLine("WHEN NOT MATCHED BY TARGET");
            sqlMergeSecuritySb.AppendLine("THEN INSERT (SourceId, EntityId, SourceType, Permission)");
            sqlMergeSecuritySb.AppendLine("VALUES (src.SourceId, src.EntityId, src.SourceType, src.Permission)");
            sqlMergeSecuritySb.AppendLine("WHEN NOT MATCHED BY SOURCE");
            sqlMergeSecuritySb.AppendLine("THEN DELETE;");

            try
            {
                //await _context.Connection.ExecuteAsync(
                //                sqlMergeSecuritySb.ToString());
                //results.Add($"Success execute sql merge");

            }
            catch (Exception ex)
            {
                results.Add($"Fail execute sql merge. SqlQuery: {sqlMergeSecuritySb}");
            }

            #endregion
            results.Add($"Success all : {targetTableName}");
        }
        catch (Exception ex)
        {
            results.Add($"Failed any: {ex}");
        }

        return results;
    }

    public async Task<List<PermissionDto>> GetListPermission()
    {
        return _context.Permissions.ProjectToType<PermissionDto>().ToList();
    }
}