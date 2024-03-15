using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using JPL.NetCoreUtility.Infrastructure.Persistence.Initialization;
using JPL.NetCoreUtility.Infrastructure.Persistence.Context;
using JPL.NetCoreUtility.Application.Common.Interfaces;
using JPL.NetCoreUtility.Domain.Grant;

namespace JPL.NetCoreUtility.Infrastructure.Grant;

public class ServiceSeeder : ICustomSeeder
{
    private readonly ISerializerService _serializerService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ServiceSeeder> _logger;

    public ServiceSeeder(ISerializerService serializerService, ILogger<ServiceSeeder> logger, ApplicationDbContext db)
    {
        _serializerService = serializerService;
        _logger = logger;
        _db = db;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        string entityName = nameof(_db.Services);
        string? path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!_db.Services.Any())
        {
            _logger.LogInformation($"Started to Seed {entityName}.");

            string data = await File.ReadAllTextAsync(path + $"/Grant/{entityName}.json", cancellationToken);
            var items = _serializerService.Deserialize<List<Service>>(data);

            if (items != null)
            {
                foreach (var item in items)
                {
                    await _db.Services.AddAsync(item, cancellationToken);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"Seeded {entityName}.");
        }
    }
}