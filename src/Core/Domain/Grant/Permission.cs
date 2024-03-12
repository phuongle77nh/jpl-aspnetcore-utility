namespace JPL.NetCoreUtility.Domain.Grant;

public class Permission : AuditableEntity, IAggregateRoot
{
    public string PermissionName { get; set; }
    public int Value { get; set; }

    public Permission(string permissionName, int value)
    {
        PermissionName = permissionName;
        Value = value;
    }
}