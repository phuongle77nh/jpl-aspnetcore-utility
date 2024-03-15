using JPL.NetCoreUtility.Application.UserScope;
using JPL.NetCoreUtility.Shared.Multitenancy;
using System.Text.Json.Serialization;

namespace JPL.NetCoreUtility.Application.Grant;

public class SecuredItem
{
    public Guid EntityId { get; set; }
    public string EntityName { get; set; }

    public int Permission { get; set; }
    public string PermissionName { get; set; }
}

public class SecuredDataDto
{
    public List<SecuredItem> SecuredItems { get; set; }
    public List<SecuredItem> NoPermissionItems { get; set; }

    public SecuredDataDto(List<SecuredItem> securedItems)
    {
        SecuredItems = securedItems;
    }

    public SecuredDataDto()
    {
        SecuredItems = new List<SecuredItem>();
    }
}

public class GetSecuredItemRequest : IRequest<SecuredDataDto>
{
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string ServiceName { get; set; }
    public string ServiceEntityName { get; set; }
    public string ServiceEntitySchema { get; set; }
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
        string defaultTenant = "jpl";
        Guid userId = Guid.Empty;
        if (request.UserId.HasValue)
        {
            userId = request.UserId.Value;
        }
        else if (request.UserEmail != null)
        {
            var userInfos = await _repository.ExecStoredProcAsync<UserInfoDto>(SprocConstants.GetUsers, new { request.UserEmail, TenantId = defaultTenant });
            if (userInfos != null && userInfos.Any())
            {
                userId = userInfos.First().UserId;
            }

        }

        if (userId == Guid.Empty) { return new SecuredDataDto(); }

        var securedItems = await _repository.ExecStoredProcAsync<SecuredItem>("[Grant].[GetPermissionByUserId]", new
        {
            ServiceName = request.ServiceName,
            InputTableName = request.ServiceEntityName,
            InputTableSchema = request.ServiceEntitySchema,
            InputUserId = userId
        });

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
                var users = await _repository.ExecStoredProcAsync<UserInfoDto>(SprocConstants.GetUsers, new { TenantId = defaultTenant });
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

            var result = new SecuredDataDto(securedItems);
            result.NoPermissionItems = noPermissionList;
            return result;
        }

        return new SecuredDataDto(new List<SecuredItem>());
    }
}