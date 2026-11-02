using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SevaOne.Business.AiChat.Interfaces;
using SevaOne.Business.AiChat.Services;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Business.Discovery.Services;
using SevaOne.Business.Orchestration.Interfaces;
using SevaOne.Business.Orchestration.Services;
using SevaOne.Business.Portal.Interfaces;
using SevaOne.Business.Portal.Services;
using SevaOne.Business.SevaSindhu.Interfaces;
using SevaOne.Business.SevaSindhu.Services;
using SevaOne.Business.Status.Interfaces;
using SevaOne.Business.Status.Services;

namespace SevaOne.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddSevaOneBusiness(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);
        services.AddMemoryCache();

        services.AddScoped<ICitizenSessionService, CitizenSessionService>();
        services.AddScoped<IPortalAuthService, PortalAuthService>();
        services.AddScoped<IOperatorAuthService, OperatorAuthService>();
        services.AddScoped<IGovAdminAuthService, GovAdminAuthService>();
        services.AddScoped<IGovAdminClaimService, GovAdminClaimService>();
        services.AddScoped<IGovAdminManagementService, GovAdminManagementService>();
        services.AddScoped<IGovAdminBulkImportService, GovAdminBulkImportService>();
        services.AddScoped<IOperatorManagementService, OperatorManagementService>();
        services.AddScoped<IGovAdminActivityLogService, GovAdminActivityLogService>();
        services.AddScoped<IServiceNoticeService, ServiceNoticeService>();
        services.AddScoped<IGovNotificationService, GovNotificationService>();
        services.AddScoped<IGovDashboardService, GovDashboardService>();
        services.AddScoped<IOperatorClaimService, OperatorClaimService>();
        services.AddScoped<IOperatorCitizenBindService, OperatorCitizenBindService>();
        services.AddScoped<ICurrentApplicantResolver, CurrentApplicantResolver>();
        services.AddScoped<IAjskRedirectService, AjskRedirectService>();
        services.AddScoped<IPortalApplyService, PortalApplyService>();
        services.AddScoped<IApplyClickTrackingService, ApplyClickTrackingService>();
        services.AddScoped<IDeptTokenGate, DeptTokenGate>();
        services.AddScoped<IPortalDeptFacingService, PortalDeptFacingService>();

        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ICatalogIngestionService, CatalogIngestionService>();
        services.AddScoped<ICatalogPublishService, CatalogPublishService>();
        services.AddScoped<ICatalogHierarchyService, CatalogHierarchyService>();
        services.AddScoped<ICatalogVisibilityService, CatalogVisibilityService>();
        services.AddScoped<ITokenIntegrationAdminService, TokenIntegrationAdminService>();
        services.AddScoped<ITokenAcknowledgementService, TokenAcknowledgementService>();
        services.AddScoped<ITokenStatusService, TokenStatusService>();
        services.AddScoped<IKaveriTokenService, KaveriTokenService>();
        services.AddScoped<IKaveriSsoService, KaveriSsoService>();
        services.AddScoped<IEdcsApiTokenService, EdcsApiTokenService>();
        services.AddScoped<IEdcsApiFormTokenService, EdcsApiFormTokenService>();
        services.AddScoped<IEdcsApiAckService, EdcsApiAckService>();
        services.AddScoped<TokenApplicationWriter>();
        services.AddScoped<ITokenVerificationService, TokenVerificationService>();
        services.AddScoped<IDeptPortalService, DeptPortalService>();
        services.AddScoped<ICatalogAdminAuthService, CatalogAdminAuthService>();
        services.AddScoped<ICatalogManagementService, CatalogManagementService>();
        services.AddScoped<IDepartmentSessionService, DepartmentSessionService>();
        services.AddScoped<ISubmitApplicationService, SubmitApplicationService>();
        services.AddScoped<IApplicationStatusService, ApplicationStatusService>();
        services.AddScoped<ISevaSindhuEscomService, SevaSindhuEscomService>();
        services.AddScoped<ISevaSindhuCatalog2Service, SevaSindhuCatalog2Service>();
        services.AddScoped<IAiChatProxyService, AiChatProxyService>();

        return services;
    }
}
