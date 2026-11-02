using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SevaOne.Infrastructure.Connectors;

public static class ConnectorRegistration
{
    public static readonly string[] GenericDepartments =
    [
        "transport", "urban", "health", "food", "labour", "wcd",
        "social-welfare", "education", "rdpr", "home"
    ];

    public static IServiceCollection AddDepartmentConnectors(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ConnectorOptions>(config.GetSection(ConnectorOptions.SectionName));
        var options = config.GetSection(ConnectorOptions.SectionName).Get<ConnectorOptions>() ?? new ConnectorOptions();
        var hubUrl = options.HubBaseUrl.TrimEnd('/') + "/";

        foreach (var code in GenericDepartments)
        {
            var captured = code;
            services.AddHttpClient($"dept-{captured}", client =>
            {
                client.BaseAddress = new Uri(hubUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            services.AddTransient<IDepartmentConnector>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                return new GenericHttpDepartmentConnector(captured, factory.CreateClient($"dept-{captured}"));
            });
        }

        services.AddSingleton<IDepartmentConnector>(_ => new StubDepartmentConnector("commercial-taxes"));
        services.AddScoped<IDepartmentConnectorRegistry, DepartmentConnectorRegistry>();
        return services;
    }
}
