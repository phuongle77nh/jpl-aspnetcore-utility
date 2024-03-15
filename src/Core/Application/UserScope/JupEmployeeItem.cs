namespace JPL.NetCoreUtility.Application.UserScope;

public enum EmployeeStatus
{
    In = 1,
    Out = 2,
    Recruited = 3,
    TemporaryAbsence = 4,
    Suspended = 5,
    ExitConfirmed = 6,
}

public class JupEmployeeItem
{
    public Guid UserId { get; set; }
    public Guid DepartmentId { get; set; }
    public string Department { get; set; }
    public string JobTitle { get; set; }
    public string BusinessEmail { get; set; }
    public double EmployeeStatus { get; set; }
    public string TenantId { get; set; }

    public EmployeeStatus GetEmployeeStatus()
    {
        int value = (int)EmployeeStatus;
        return (EmployeeStatus)value;
    }
}

public class SetUserScopeInput
{
    public string TenantId { get; set; }
    public string Role { get; set; }
    public string Scope { get; set; }
    public string Username { get; set; }
}
