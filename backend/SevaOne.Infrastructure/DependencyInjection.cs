using SevaOne.Infrastructure.ExternalHttp;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SevaOne.Domain.Connection;
using SevaOne.Infrastructure.AiBackend;
using SevaOne.Infrastructure.Caching;
using SevaOne.Infrastructure.Connectors;
using SevaOne.Infrastructure.ExternalAuth;
using SevaOne.Infrastructure.Messaging;
using SevaOne.Infrastructure.Repositories.Audit;
using SevaOne.Infrastructure.Repositories.Common;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Repositories.GovAdmin;
using SevaOne.Infrastructure.Repositories.Notification;
using SevaOne.Infrastructure.Repositories.Orchestration;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Repositories.Status;
using SevaOne.Infrastructure.Security;
using StackExchange.Redis;

namespace SevaOne.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSevaOneInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();
        services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();

        services.AddScoped<IPortalRepository, PortalRepository>();
        services.AddScoped<IGovAdminImportRepository, GovAdminImportRepository>();
        services.AddScoped<IServiceNoticeRepository, ServiceNoticeRepository>();
        services.AddScoped<IGovNotificationRepository, GovNotificationRepository>();
        services.AddScoped<IGramaOneCentreRepository, GramaOneCentreRepository>();
        services.AddScoped<IDigiLockerDocumentRepository, DigiLockerDocumentRepository>();
        services.Configure<SevaOne.Infrastructure.Storage.ObjectStorageOptions>(config.GetSection(SevaOne.Infrastructure.Storage.ObjectStorageOptions.SectionName));
        services.AddSingleton<SevaOne.Infrastructure.Storage.IObjectStorage, SevaOne.Infrastructure.Storage.MinioObjectStorage>();
        services.AddScoped<IGovDashboardRepository, GovDashboardRepository>();
        services.AddScoped<IGovAdminActivityLogRepository, GovAdminActivityLogRepository>();
        services.AddScoped<IDiscoveryRepository, DiscoveryRepository>();
        services.AddScoped<ICatalogManagementRepository, CatalogManagementRepository>();
        services.AddScoped<ICatalogVisibilityRepository, CatalogVisibilityRepository>();
        services.AddScoped<ITokenIntegrationRepository, TokenIntegrationRepository>();
        services.AddScoped<ICatalogIngestionRepository, CatalogIngestionRepository>();
        services.AddScoped<IOrchestrationRepository, OrchestrationRepository>();
        services.AddScoped<IStatusRepository, StatusRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        var redisConnection = config.GetConnectionString("Redis") ?? "localhost:6379";
        var mux = ConnectionMultiplexer.Connect(redisConnection);
        services.AddSingleton<IConnectionMultiplexer>(mux);
        services.AddSingleton<ISevaSessionStore, RedisSevaSessionStore>();
        services.AddSingleton<IOperatorSessionStore, RedisOperatorSessionStore>();
        services.AddSingleton<IAdminSessionStore, RedisAdminSessionStore>();
        services.AddSingleton<IGovAdminSessionStore, RedisGovAdminSessionStore>();
        services.AddDataProtection().PersistKeysToStackExchangeRedis(mux, "seva-dp-keys");

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPayloadProtector, DataProtectionPayloadProtector>();
        services.AddSingleton<IApplyRedirectProtector, DataProtectionApplyRedirectProtector>();
        services.AddSingleton<ITokenStatusSecretProtector, DataProtectionTokenStatusSecretProtector>();
        services.AddSingleton<ITokenCache, RedisTokenCache>();

        services.Configure<JwsOptions>(config.GetSection(JwsOptions.SectionName));
        services.AddSingleton<IJwsTokenService, JwsTokenService>();

        services.Configure<Wso2CitizenIssuerOptions>(config.GetSection(Wso2CitizenIssuerOptions.SectionName));
        services.AddHttpClient<IWso2CitizenIdentityClient, Wso2CitizenIdentityClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(_ =>
            {
                var opts = config.GetSection(Wso2CitizenIssuerOptions.SectionName).Get<Wso2CitizenIssuerOptions>() ?? new Wso2CitizenIssuerOptions();
                var handler = new HttpClientHandler();
                if (opts.AcceptInsecureCertificates)
                {
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                return handler;
            });

        services.AddHttpClient<IWso2TokenIntrospectionCache, Wso2TokenIntrospectionCache>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .ConfigurePrimaryHttpMessageHandler(_ =>
            {
                var opts = config.GetSection(Wso2CitizenIssuerOptions.SectionName).Get<Wso2CitizenIssuerOptions>() ?? new Wso2CitizenIssuerOptions();
                var handler = new HttpClientHandler();
                if (opts.AcceptInsecureCertificates)
                {
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                return handler;
            });

        services.Configure<Wso2OperatorIssuerOptions>(config.GetSection(Wso2OperatorIssuerOptions.SectionName));
        services.AddHttpClient<IWso2OperatorIdentityClient, Wso2OperatorIdentityClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(_ =>
            {
                // Shares Wso2CitizenIssuerOptions.AcceptInsecureCertificates -- same WSO2 IS
                // instance, one TLS-trust decision for it, not a separate operator-specific one.
                var opts = config.GetSection(Wso2CitizenIssuerOptions.SectionName).Get<Wso2CitizenIssuerOptions>() ?? new Wso2CitizenIssuerOptions();
                var handler = new HttpClientHandler();
                if (opts.AcceptInsecureCertificates)
                {
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                return handler;
            });

        services.Configure<Wso2GovAdminIssuerOptions>(config.GetSection(Wso2GovAdminIssuerOptions.SectionName));
        services.AddHttpClient<IWso2GovAdminIdentityClient, Wso2GovAdminIdentityClient>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(_ =>
            {
                // Shares Wso2CitizenIssuerOptions.AcceptInsecureCertificates -- same WSO2 IS
                // instance, one TLS-trust decision for it, not a separate gov-admin-specific one.
                var opts = config.GetSection(Wso2CitizenIssuerOptions.SectionName).Get<Wso2CitizenIssuerOptions>() ?? new Wso2CitizenIssuerOptions();
                var handler = new HttpClientHandler();
                if (opts.AcceptInsecureCertificates)
                {
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                return handler;
            });

        services.Configure<DigiLockerOptions>(config.GetSection(DigiLockerOptions.SectionName));
        services.AddHttpClient<IDigiLockerClient, DigiLockerClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.Configure<TokenStatusHttpOptions>(config.GetSection(TokenStatusHttpOptions.SectionName));
        services.Configure<EdcsApiOptions>(config.GetSection(EdcsApiOptions.SectionName));
        services.AddHttpClient(TokenStatusHttpClient.ClientName, client => client.Timeout = TimeSpan.FromSeconds(12))
            .ConfigurePrimaryHttpMessageHandler(sp => TokenStatusHttpClient.CreateHandler(sp.GetRequiredService<IOptions<TokenStatusHttpOptions>>()));
        services.AddSingleton<ITokenStatusHttpClient, TokenStatusHttpClient>();

        services.Configure<SakalaOptions>(config.GetSection(SakalaOptions.SectionName));
        services.AddHttpClient<ISakalaClient, SakalaClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.Configure<SevaSindhuEscomOptions>(config.GetSection(SevaSindhuEscomOptions.SectionName));
        services.AddHttpClient<ISevaSindhuEscomClient, SevaSindhuEscomClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        services.Configure<SevaSindhuCatalog2Options>(config.GetSection(SevaSindhuCatalog2Options.SectionName));
        services.AddHttpClient<ISevaSindhuCatalog2Client, SevaSindhuCatalog2Client>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        // ai_backend (RAG chat/discovery + voice transcription) -- see AiBackendOptions for
        // where BaseUrl actually points in this deployment.
        services.Configure<AiBackendOptions>(config.GetSection(AiBackendOptions.SectionName));
        services.AddHttpClient<IAiBackendClient, AiBackendClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // ai_backend's own health/readiness/reingest routes -- kept on a separate client and
        // BaseUrl from IAiBackendClient above, since chat now targets the "merged" sevaone-rag
        // service, which never implemented /api/v1/ready or /api/v1/ai-knowledge/refresh.
        services.Configure<AiIngestionOptions>(config.GetSection(AiIngestionOptions.SectionName));
        services.AddHttpClient<IAiIngestionClient, AiIngestionClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Optional read-only GPU-host telemetry. An empty BaseUrl is intentional: the existing
        // AI status route remains useful before the separate monitor sidecar is deployed.
        services.Configure<AiMonitorOptions>(config.GetSection(AiMonitorOptions.SectionName));
        services.AddHttpClient<IAiMonitorClient, AiMonitorClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        services.Configure<GovAdminAiAssistantOptions>(config.GetSection(GovAdminAiAssistantOptions.SectionName));
        services.AddHttpClient<IGovAdminAiAssistantClient, GovAdminAiAssistantClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(120);
        });

        // No HttpClient: AjskRedirectService builds a plain redirect URL, no token API call.
        services.Configure<AjskRedirectOptions>(config.GetSection(AjskRedirectOptions.SectionName));

        // Karnataka's Mobile Seva SMS gateway, once MobileSevaSms:Username is configured (real
        // environments only, via secret/.env) -- falls back to LoggedSmsOtpSender otherwise so
        // local dev keeps working unchanged. See MobileSevaSmsOtpSender's doc comment for a
        // real caveat: its request-signing hash algorithm is an unverified guess.
        services.Configure<MobileSevaSmsOptions>(config.GetSection(MobileSevaSmsOptions.SectionName));
        services.AddSingleton<DevOtpInbox>();
        services.AddSingleton<LoggedSmsOtpSender>();
        services.AddHttpClient<MobileSevaSmsOtpSender>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
        });
        services.AddSingleton<ISmsOtpSender>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MobileSevaSmsOptions>>().Value;
            return string.IsNullOrWhiteSpace(options.Username)
                ? sp.GetRequiredService<LoggedSmsOtpSender>()
                : sp.GetRequiredService<MobileSevaSmsOtpSender>();
        });

        services.Configure<KafkaOptions>(config.GetSection(KafkaOptions.SectionName));
        services.AddSingleton<IKafkaProducer, KafkaProducer>();
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();

        services.AddDepartmentConnectors(config);

        return services;
    }
}
