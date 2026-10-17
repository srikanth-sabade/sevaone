using SevaOne.Domain.Models;

namespace SevaOne.Business.Portal.Services;

/// <summary>Which of the four apply mechanisms a scheme resolves to. Extracted from
/// PortalApplyService.MintApplyTokenAsync's inline conditions so ApplyClickTrackingService can
/// classify a click's IsHomepageFallback bookkeeping without duplicating (or drifting from)
/// the real mint logic.</summary>
internal enum ApplyRoute
{
    AjskRedirect,
    SakalaTokenRedirect,
    HomepageFallback,
    RealDeeplinkMint,
}

internal static class ApplyRouteClassifier
{
    public static ApplyRoute Classify(Scheme scheme, Department department, bool assistedMode)
    {
        if (assistedMode && scheme.IntegrationMethod == "ajsk")
        {
            return ApplyRoute.AjskRedirect;
        }

        if (assistedMode
            && scheme.IntegrationMethod is "sakala_token" or "sakala_token_escom"
            && scheme.SevasindhuServiceId is { Length: > 0 })
        {
            return ApplyRoute.SakalaTokenRedirect;
        }

        // A department that isn't actually wired up to redeem a SevaOne token (no real
        // IDepartmentConnector, nothing registered against /dept/user-details -- see
        // Department.IsIntegrated) can never consume the JWS this would otherwise mint. The
        // catalog import populates DeeplinkCitizenUrl from CSV data independently of that flag,
        // so a non-integrated department can still have one set -- it's just an informational
        // link in that case, not a real apply endpoint, so treat it the same as a plain
        // homepage redirect instead of gating it behind a login nothing on the other end needs.
        if (!department.IsIntegrated)
        {
            return ApplyRoute.HomepageFallback;
        }

        var deeplinkUrl = assistedMode
            ? (string.IsNullOrWhiteSpace(scheme.DeeplinkOperatorUrl) ? scheme.DeeplinkCitizenUrl : scheme.DeeplinkOperatorUrl)
            : scheme.DeeplinkCitizenUrl;

        return string.IsNullOrWhiteSpace(deeplinkUrl) ? ApplyRoute.HomepageFallback : ApplyRoute.RealDeeplinkMint;
    }

    /// <summary>Best URL for a HomepageFallback route -- the scheme's own deeplink when the
    /// catalog import happened to populate one (the common case for a non-integrated
    /// department, see Classify), otherwise its plain homepage.</summary>
    public static string? FallbackUrl(Scheme scheme, bool assistedMode)
    {
        var deeplinkUrl = assistedMode
            ? (string.IsNullOrWhiteSpace(scheme.DeeplinkOperatorUrl) ? scheme.DeeplinkCitizenUrl : scheme.DeeplinkOperatorUrl)
            : scheme.DeeplinkCitizenUrl;
        return string.IsNullOrWhiteSpace(deeplinkUrl) ? scheme.HomepageUrl : deeplinkUrl;
    }
}
