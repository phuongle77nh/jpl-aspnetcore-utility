namespace JPL.NetCoreUtility.Application.UserScope;

public class UserScopeDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public Guid ScopeId { get; set; }
    public string ScopeName { get; set; } = string.Empty;
    public bool IsMain { get; set; }

    public Guid GetUserId()
    {
        return new Guid(UserId);
    }
}
