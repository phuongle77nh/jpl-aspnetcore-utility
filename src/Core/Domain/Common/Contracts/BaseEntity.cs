using System.ComponentModel.DataAnnotations.Schema;
using MassTransit;

namespace JPL.NetCoreUtility.Domain.Common.Contracts;

public abstract class BaseEntity : BaseEntity<DefaultIdType>
{
    protected BaseEntity() => Id = NewId.Next().ToGuid();
}

public abstract class BaseEntity<TId> : IEntity<TId>
{
    public TId Id { get; set; } = default!;

    [NotMapped]
    public List<DomainEvent> DomainEvents { get; } = new();
}