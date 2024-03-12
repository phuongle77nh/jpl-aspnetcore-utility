namespace JPL.NetCoreUtility.Domain.Grant;

public class ServiceEntity : AuditableEntity, IAggregateRoot
{
    public string TableName { get; set; }
    public string TableSchema { get; set; }
    public string PrimaryColumnName { get; set; }

    /// <summary>
    /// Query get user own their record.
    /// </summary>
    public string MyselfQuery { get; set; }

    public Guid ServiceId { get; set; }
    public virtual Service Service { get; set; } = default!;

    public virtual ICollection<SecurityAttribute> SecurityAttributes { get; set; }
    public virtual ICollection<Policy> Policies { get; set; }

    public ServiceEntity()
    {
        TableName = string.Empty;
        TableSchema = string.Empty;
        PrimaryColumnName = string.Empty;
        SecurityAttributes = new List<SecurityAttribute>();
        Policies = new List<Policy>();
    }
}