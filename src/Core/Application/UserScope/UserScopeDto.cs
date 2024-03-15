namespace JPL.NetCoreUtility.Application.UserScope;

public class UserScopeDto
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public string RoleId { get; set; }
    public string RoleName { get; set; }
    public Guid ScopeId { get; set; }
    public string ScopeName { get; set; }
    public bool IsMain { get; set; }

    public Guid GetUserId()
    {
        return new Guid(UserId);
    }
}
