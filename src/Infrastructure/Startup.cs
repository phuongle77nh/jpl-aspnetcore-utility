using System.Reflection;
using System.Runtime.CompilerServices;
using JPL.NetCoreUtility.Infrastructure.ApplicationInsight;
using JPL.NetCoreUtility.Infrastructure.Auth;
using JPL.NetCoreUtility.Infrastructure.BackgroundJobs;
using JPL.NetCoreUtility.Infrastructure.Caching;
using JPL.NetCoreUtility.Infrastructure.Common;
using JPL.NetCoreUtility.Infrastructure.Cors;
using JPL.NetCoreUtility.Infrastructure.FileStorage;
using JPL.NetCoreUtility.Infrastructure.Localization;
using JPL.NetCoreUtility.Infrastructure.Mailing;
using JPL.NetCoreUtility.Infrastructure.Mapping;
using JPL.NetCoreUtility.Infrastructure.Middleware;
using JPL.NetCoreUtility.Infrastructure.Multitenancy;
using JPL.NetCoreUtility.Infrastructure.Notifications;
using JPL.NetCoreUtility.Infrastructure.OpenApi;
using JPL.NetCoreUtility.Infrastructure.Persistence;
using JPL.NetCoreUtility.Infrastructure.Persistence.Initialization;
using JPL.NetCoreUtility.Infrastructure.SecurityHeaders;
using JPL.NetCoreUtility.Infrastructure.Validations;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Infrastructure.Test")]

namespace JPL.NetCoreUtility.Infrastructure;

public static class Startup
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var applicationAssembly = typeof(JPL.NetCoreUtility.Application.Startup).GetTypeInfo().Assembly;
        MapsterSettings.Configure();
        return services
            .AddApiVersioning()
            .AddAuth(config)
            .AddBackgroundJobs(config)
            .AddCaching(config)
            .AddCorsPolicy(config)
            .AddExceptionMiddleware()
            .AddBehaviours(applicationAssembly)
            .AddHealthCheck()
            .AddPOLocalization(config)
            .AddMailing(config)
            .AddMediatR(Assembly.GetExecutingAssembly())
            .AddMultitenancy()
            .AddNotifications(config)
            .AddOpenApiDocumentation(config)
            .AddPersistence()
            .AddRequestLogging(config)
            .AddRouting(options => options.LowercaseUrls = true)
            .AddApplicationInsight(config)
            .AddServices();
    }

    private static IServiceCollection AddApiVersioning(this IServiceCollection services) =>
        services.AddApiVersioning(config =>
        {
            config.DefaultApiVersion = new ApiVersion(1, 0);
            config.AssumeDefaultVersionWhenUnspecified = true;
            config.ReportApiVersions = true;
        });

    private static IServiceCollection AddHealthCheck(this IServiceCollection services) =>
        services.AddHealthChecks().AddCheck<TenantHealthCheck>("Tenant").Services;

    public static async Task InitializeDatabasesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        // Create a new scope to retrieve scoped services
        using var scope = services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>()
            .InitializeDatabasesAsync(cancellationToken);
    }

    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder builder, IConfiguration config) =>
        builder
            .UseRequestLocalization()
            .UseStaticFiles()
            .UseSecurityHeaders(config)
            .UseFileStorage()
            .UseExceptionMiddleware()
            .UseRouting()
            .UseCorsPolicy()
            .UseAuthentication()
            .UseCurrentUser()
            .UseMultiTenancy()
            .UseAuthorization()
            .UseRequestLogging(config)
            .UseHangfireDashboard(config)
            .UseOpenApiDocumentation(config);

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapControllers().RequireAuthorization();
        builder.MapHealthCheck();
        builder.MapNotifications();
        return builder;
    }

    private static IEndpointConventionBuilder MapHealthCheck(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapHealthChecks("/api/health");
}