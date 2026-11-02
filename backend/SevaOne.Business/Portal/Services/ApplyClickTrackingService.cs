using SevaOne.Business.Common;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Portal.Interfaces;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Portal.Services;

/// <summary>
/// The "just count the click" half of what PortalApplyService.MintApplyTokenAsync used to do
/// alone -- a raw interest signal, not a completed-application record. Deliberately anonymous:
/// discovery.service_apply_track.citizen_id is a nullable column precisely so this can run for
/// a citizen who is just browsing, not only one who has logged in. Real identity (and the
/// audit-log "Recent Activity" trail) is only ever required by the separate mint path, and only
/// for the one route that actually needs to embed SevaOneUserId in a real department token.
///
/// Never throws: a lookup miss (an AI-resolved service that doesn't map to a real catalog row,
/// for example) is reported back as Tracked:false, not an error -- this must never block a
/// citizen's navigation over a failed count.
/// </summary>
public sealed class ApplyClickTrackingService : IApplyClickTrackingService
{
    private readonly IDiscoveryRepository _discovery;
    private readonly IPortalRepository _portal;
    private readonly ICurrentCitizen _current;
    private readonly IClock _clock;
    private readonly ICurrentOperator _operator;
    private readonly IPortalApplyService _apply;

    public ApplyClickTrackingService(IDiscoveryRepository discovery, IPortalRepository portal, ICurrentCitizen current, IClock clock, ICurrentOperator currentOperator, IPortalApplyService apply)
    {
        _discovery = discovery;
        _portal = portal;
        _current = current;
        _clock = clock;
        _operator = currentOperator;
        _apply = apply;
    }

    public async Task<TrackApplyClickResult> TrackAsync(TrackApplyClickRequest request, CancellationToken ct)
    {
        var scheme = await _discovery.GetSchemeByDepartmentAndServiceCodeAsync(request.DepartmentCode, request.ServiceCode, ct);
        if (scheme is null)
        {
            return new TrackApplyClickResult(false);
        }

        var department = await _discovery.GetDepartmentByIdAsync(scheme.DepartmentId, ct);
        if (department is null)
        {
            return new TrackApplyClickResult(false);
        }

        var assistedMode = request.AssistedMode && _operator.IsAuthenticated;
        // Attribute assisted clicks only to the citizen bound to the authenticated session.
        var citizenId = assistedMode ? _operator.ActiveCitizenId : _current.CitizenId;
        var route = ApplyRouteClassifier.Classify(scheme, department, assistedMode);

        await _discovery.InsertServiceApplyTrackAsync(new ServiceApplyTrack
        {
            Id = Guid.NewGuid(),
            SchemeId = scheme.Id,
            DepartmentId = department.Id,
            CitizenId = citizenId,
            AssistedMode = assistedMode,
            IsHomepageFallback = route == ApplyRoute.HomepageFallback,
            CreatedAt = _clock.UtcNow
        }, ct);

        return new TrackApplyClickResult(true);
    }

    /// <summary>See TrackAiApplyClickResult's own doc comment for the overall shape. A found
    /// match logs the click here directly (mirroring TrackAsync above, source="ai") and then
    /// delegates URL resolution to CreateApplyTokenAsync -- the exact same login/homepage-
    /// fallback/deep-link-mint logic the catalogue flow already uses, so an AI-driven apply is
    /// indistinguishable from a catalogue one from that point on. A miss still logs a
    /// scheme/department-less row (a genuine "AI apply interest" signal even when it can't roll
    /// up into any specific service's ranking) and returns Matched=false for the frontend to
    /// fall back to sevaone-rag's own apply_url.</summary>
    public async Task<TrackAiApplyClickResult> ResolveAndTrackAiApplyAsync(TrackAiApplyClickRequest request, CancellationToken ct)
    {
        var match = await _discovery.FindActiveSchemeByNameAsync(request.ServiceName, request.DepartmentCode, request.OperatorHint, ct);

        var assistedMode = request.AssistedMode && _operator.IsAuthenticated;
        var citizenId = assistedMode ? _operator.ActiveCitizenId : _current.CitizenId;

        if (match is null)
        {
            await _discovery.InsertServiceApplyTrackAsync(new ServiceApplyTrack
            {
                Id = Guid.NewGuid(),
                SchemeId = null,
                DepartmentId = null,
                CitizenId = citizenId,
                AssistedMode = assistedMode,
                IsHomepageFallback = false,
                Source = "ai",
                CreatedAt = _clock.UtcNow
            }, ct);
            return new TrackAiApplyClickResult(Matched: false);
        }

        await _discovery.InsertServiceApplyTrackAsync(new ServiceApplyTrack
        {
            Id = Guid.NewGuid(),
            SchemeId = match.Id,
            DepartmentId = match.DepartmentRowId,
            CitizenId = citizenId,
            AssistedMode = assistedMode,
            IsHomepageFallback = false, // Resolved below by CreateApplyTokenAsync; not known yet at this point.
            Source = "ai",
            CreatedAt = _clock.UtcNow
        }, ct);

        var outcome = assistedMode && request.SevaOneUserId is { } sevaOneUserId
            ? await AssistedApplyAsync(match.DepartmentCode, match.ServiceCode, sevaOneUserId, ct)
            : await _apply.CreateApplyTokenAsync(new CreateApplyTokenRequest(match.DepartmentCode, match.ServiceCode), ct);

        return new TrackAiApplyClickResult(Matched: true, outcome.RequiresLogin, outcome.ApplyUrl, outcome.ExpiresAt, outcome.IsHomepageFallback);
    }

    private async Task<ApplyOutcomeDto> AssistedApplyAsync(string departmentCode, string serviceCode, long sevaOneUserId, CancellationToken ct)
    {
        var minted = await _apply.CreateAssistedApplyTokenAsync(new CreateAssistedApplyTokenRequest(departmentCode, serviceCode, sevaOneUserId), ct);
        return new ApplyOutcomeDto(RequiresLogin: false, minted.ApplyUrl, minted.ExpiresAt, minted.IsHomepageFallback);
    }
}
