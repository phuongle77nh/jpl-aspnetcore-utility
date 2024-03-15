using JPL.NetCoreUtility.Application.UserScope;
using JPL.NetCoreUtility.Shared.Multitenancy;
using System.Text.Json.Serialization;

namespace JPL.NetCoreUtility.Application.Grant;

public class SecuredItem
{
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;

    public int Permission { get; set; }
    public string PermissionName { get; set; } = string.Empty;
}

public class SecuredDataDto
{
    public List<SecuredItem> SecuredItems { get; set; }
    public List<SecuredItem> NoPermissionItems { get; set; }

    public SecuredDataDto(List<SecuredItem> securedItems)
    {
        SecuredItems = securedItems;
        NoPermissionItems = new List<SecuredItem>();
    }

    public SecuredDataDto()
    {
        SecuredItems = new List<SecuredItem>();
        NoPermissionItems = new List<SecuredItem>();
    }
}

public class GetSecuredItemRequest : IRequest<SecuredDataDto>
{
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceEntityName { get; set; } = string.Empty;
    public string ServiceEntitySchema { get; set; } = string.Empty;
    public GetSecuredItemRequest(DefaultIdType? userId, string? userEmail, string serviceName, string serviceEntityName, string serviceEntitySchema)
    {
        UserId = userId;
        UserEmail = userEmail;
        ServiceName = serviceName;
        ServiceEntityName = serviceEntityName;
        ServiceEntitySchema = serviceEntitySchema;
    }
}

public class GetSecuredItemRequestHandler : IRequestHandler<GetSecuredItemRequest, SecuredDataDto>
{
    private readonly IDapperRepository _repository;
    private readonly IGrantService _service;

    public GetSecuredItemRequestHandler(IDapperRepository repository, IGrantService service)
    {
        _repository = repository;
        _service = service;
    }

    public async Task<SecuredDataDto> Handle(GetSecuredItemRequest request, CancellationToken cancellationToken)
    {
        const string defaultTenant = "jpl";
        Guid userId = Guid.Empty;
        if (request.UserId.HasValue)
        {
            userId = request.UserId.Value;
        }
        else if (request.UserEmail != null)
        {
            var userInfos = await _repository.ExecStoredProcAsync<UserInfoDto>(
                sql: SprocConstants.GetUsers,
                param: new { request.UserEmail, TenantId = defaultTenant },
                cancellationToken: cancellationToken);
            if (userInfos?.Any() == true)
            {
                userId = userInfos.First().UserId;
            }
        }

        if (userId == Guid.Empty) { return new SecuredDataDto(); }

        var securedItems = await _repository.ExecStoredProcAsync<SecuredItem>(
            sql: "[Grant].[GetPermissionByUserId]",
            param: new
                {
                    request.ServiceName,
                    InputTableName = request.ServiceEntityName,
                    InputTableSchema = request.ServiceEntitySchema,
                    InputUserId = userId
                },
            cancellationToken: cancellationToken);

        if (securedItems != null)
        {
            var permissions = await _service.GetListPermission();
            foreach (var item in securedItems)
            {
                item.PermissionName = permissions.FirstOrDefault(x => x.Value == item.Permission)?.PermissionName ?? "N/A";
            }

            var noPermissionList = new List<SecuredItem>();
            if (request.ServiceEntityName == "User" && request.ServiceEntitySchema == "User")
            {
                var users = await _repository.ExecStoredProcAsync<UserInfoDto>(
                    sql: SprocConstants.GetUsers,
                    param: new { TenantId = defaultTenant },
                    cancellationToken: cancellationToken);
                foreach (var user in users)
                {
                    var item = securedItems.FirstOrDefault(x => x.EntityId == user.UserId);

                    if (item != null)
                    {
                        item.EntityName = user?.WorkEmail ?? "not found email";
                        item.PermissionName = permissions.FirstOrDefault(x => x.Value == item.Permission)?.PermissionName ?? "N/A";
                    }
                    else
                    {
                        noPermissionList.Add(new SecuredItem { EntityId = user.UserId, PermissionName = "No permission", EntityName = user?.WorkEmail ?? "not found email" });
                    }
                }
            }

            return new SecuredDataDto(securedItems)
            {
                NoPermissionItems = noPermissionList
            };
        }

        return new SecuredDataDto(new List<SecuredItem>());
    }
}