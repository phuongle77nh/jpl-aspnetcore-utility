namespace JPL.NetCoreUtility.Application.UserScope;

public class UserInfoDto
{
    public Guid UserId { get; set; }
    public string WorkEmail { get; set; }
    public string LastName { get; set; }
    public string FirstName { get; set; }
}
