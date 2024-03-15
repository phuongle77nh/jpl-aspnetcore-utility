using Finbuckle.MultiTenant;
using JPL.NetCoreUtility.Application.Common.Events;
using JPL.NetCoreUtility.Application.Common.Interfaces;
using JPL.NetCoreUtility.Domain.Catalog;
using JPL.NetCoreUtility.Domain.Grant;
using JPL.NetCoreUtility.Infrastructure.Persistence.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JPL.NetCoreUtility.Infrastructure.Persistence.Context;

public class ApplicationDbContext : BaseDbContext
{
    public ApplicationDbContext(ITenantInfo currentTenant, DbContextOptions options, ICurrentUser currentUser, ISerializerService serializer, IOptions<DatabaseSettings> dbSettings, IEventPublisher events)
        : base(currentTenant, options, currentUser, serializer, dbSettings, events)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceEntity> ServiceEntities => Set<ServiceEntity>();
    public DbSet<SecurityAttribute> Attributes => Set<SecurityAttribute>();
    public DbSet<Permission> Permissions => Set<Permission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaNames.Catalog);
    }
}