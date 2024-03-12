using System.ComponentModel.DataAnnotations.Schema;

namespace JPL.NetCoreUtility.Domain.Grant;

//[Table("PolicyLink")]
public class PolicyLink : AuditableEntity, IAggregateRoot
{
    public Guid PolicyId { get; set; }
    public Guid EntityId { get; set; }

    /// <summary>
    /// 1 = role
    /// 2 = attribute.
    /// </summary>
    public EntityTypeEnum EntityType { get; set; }

    public string EntityFormat { get; set; }

    public virtual Policy Policy { get; set; }

    public PolicyLink()
    {
        EntityType = EntityTypeEnum.Role;
        EntityFormat = string.Empty;
    }
}