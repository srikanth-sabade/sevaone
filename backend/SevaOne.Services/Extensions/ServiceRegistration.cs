using SevaOne.Business;
using SevaOne.Infrastructure;
using SevaOne.Services.BackgroundServices;

namespace SevaOne.Services.Extensions;

public static class ServiceRegistration
{
    public static IServiceCollection AddSevaOneServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddSevaOneInfrastructure(config);
        services.AddSevaOneBusiness();

        if (!config.GetValue("Workers:Disabled", false))
        {
            services.AddHostedService<OutboxDispatcher>();
            services.AddHostedService<NotificationDispatcher>();
            services.AddHostedService<ApplicationStatusPoller>();
        }

        return services;
    }
}
