using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using UserApi.Common;

namespace UserApi.Health;

[ExcludeFromCodeCoverage]
public static class HealthCheckExtensions
{
    public static IServiceCollection AddVhHealthChecks(this IServiceCollection services)
    {
        var container = services.BuildServiceProvider();
        var connectionStrings = container.GetService<ConnectionStrings>();
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy())
            .AddRedis(connectionStrings.RedisCache, tags: new[] {"startup", "readiness"});
            
        return services;
    }
}
