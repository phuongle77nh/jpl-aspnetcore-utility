using System.Reflection;
using JPL.NetCoreUtility.Application.Common.Interfaces;
using JPL.NetCoreUtility.Infrastructure.Persistence.Context;
using JPL.NetCoreUtility.Infrastructure.Persistence.Initialization;
using Microsoft.Extensions.Logging;
using JPL.NetCoreUtility.Domain.Grant;

namespace JPL.NetCoreUtility.Infrastructure.Grant;

public class PermissionSeeder : ICustomSeeder
{
    private readonly ISerializerService _serializerService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PermissionSeeder> _logger;

    public PermissionSeeder(ISerializerService serializerService, ILogger<PermissionSeeder> logger, ApplicationDbContext db)
    {
        _serializerService = serializerService;
        _logger = logger;
        _db = db;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        string entityName = nameof(_db.Permissions);
        string? path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!_db.Permissions.Any())
        {
            _logger.LogInformation($"Started to Seed {entityName}.");

            string data = await File.ReadAllTextAsync(path + $"/Grant/{entityName}.json", cancellationToken);
            var items = _serializerService.Deserialize<List<Permission>>(data);

            if (items != null)
            {
                foreach (var item in items)
                {
                    await _db.Permissions.AddAsync(item, cancellationToken);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"Seeded {entityName}.");
        }
    }
}