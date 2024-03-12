namespace JPL.NetCoreUtility.Domain.Grant;

public class Service : AuditableEntity, IAggregateRoot
{
    public string ServiceName { get; set; }
    public string Database { get; set; }

    public virtual ICollection<ServiceEntity> ServiceEntities { get; set; }

    public Service()
    {
        ServiceName = string.Empty;
        Database = string.Empty;
        ServiceEntities = new List<ServiceEntity>();
    }
}