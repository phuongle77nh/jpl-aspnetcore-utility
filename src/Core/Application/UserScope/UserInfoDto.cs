namespace JPL.NetCoreUtility.Application.UserScope;

public class UserInfoDto
{
    public Guid UserId { get; set; }
    public string WorkEmail { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
}
