using AspNetMonsters.ApplicationInsights.AspNetCore;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace JPL.NetCoreUtility.Infrastructure.ApplicationInsight;

internal static class Startup
{
    private static readonly ILogger _logger = Log.ForContext(typeof(Startup));
    internal static IServiceCollection AddApplicationInsight(this IServiceCollection services, IConfiguration config)
    {
        var aiOptions = new Microsoft.ApplicationInsights.AspNetCore.Extensions.ApplicationInsightsServiceOptions
        {
            EnableDependencyTrackingTelemetryModule = false,
            EnablePerformanceCounterCollectionModule = false,
            AddAutoCollectedMetricExtractor = false,
            EnableAuthenticationTrackingJavaScript = false,
            EnableDiagnosticsTelemetryModule = false,
            EnableAzureInstanceMetadataTelemetryModule = false,
            EnableHeartbeat = false,
            EnableAppServicesHeartbeatTelemetryModule = false,
            EnableEventCounterCollectionModule = false,
            EnableRequestTrackingTelemetryModule = false
        };

        string? cloudRoleName = config.GetSection("ApplicationInsights")["CloudRoleName"];
        return services
            .AddApplicationInsightsTelemetry(aiOptions)
            .AddCloudRoleNameInitializer(cloudRoleName)
            .Configure<TelemetryConfiguration>((config) =>
            {
                var builder = config.DefaultTelemetrySink.TelemetryProcessorChainBuilder;

                builder.UseAdaptiveSampling(maxTelemetryItemsPerSecond: 5);
                builder.Build();
                _logger.Information($"Add application insight success. Cloud role name: {cloudRoleName}");
            });
    }
}