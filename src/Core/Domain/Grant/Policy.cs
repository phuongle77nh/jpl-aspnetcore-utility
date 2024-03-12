namespace JPL.NetCoreUtility.Domain.Grant;

public class Policy : AuditableEntity, IAggregateRoot
{
    public string PolicyName { get; set; }
    public int Permission { get; set; }

    public Guid ServiceEntityId { get; set; }

    public virtual ServiceEntity ServiceEntity { get; set; } = default!;

    public virtual ICollection<PolicyLink> PolicyLinks { get; set; }

    public Policy()
    {
        PolicyName = default!;
        PolicyLinks = new List<PolicyLink>();
    }
}