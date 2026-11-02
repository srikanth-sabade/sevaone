using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SevaOne.Services.Extensions;

public static class TracingExtensions
{
    public static IServiceCollection AddSevaOneOpenTelemetry(this IServiceCollection services, string serviceName = "sevaone")
    {
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(t => t.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation())
            .WithMetrics(m => m.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation());
        return services;
    }
}
