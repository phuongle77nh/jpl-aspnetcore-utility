namespace JPL.NetCoreUtility.Domain.Grant;

public class SecurityAttribute : AuditableEntity, IAggregateRoot
{
    public string AttributeName { get; set; }
    public string AttributeQuery { get; set; }

    public Guid ServiceEntityId { get; set; }

    public virtual ServiceEntity ServiceEntity { get; set; } = default!;

    public SecurityAttribute()
    {

    }

    public SecurityAttribute(string attributeName, string attributeQuery)
    {
        AttributeName = attributeName;
        AttributeQuery = attributeQuery;
    }
}