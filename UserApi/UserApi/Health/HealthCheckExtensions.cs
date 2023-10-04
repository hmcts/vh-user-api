using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UserApi.Health;

[ExcludeFromCodeCoverage]
public static class HealthCheckExtensions
{
    public static IServiceCollection AddVhHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy());
            
        return services;
    }
}
