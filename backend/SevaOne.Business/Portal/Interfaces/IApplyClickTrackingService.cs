using SevaOne.Business.Portal.DTOs;

namespace SevaOne.Business.Portal.Interfaces;

/// <summary>Records an apply-click as a public interest-measurement number
/// (discovery.service_apply_track), decoupled from minting a real identity-bound token.
/// Deliberately never requires authentication -- see ApplyClickTrackingService for why.</summary>
public interface IApplyClickTrackingService
{
    Task<TrackApplyClickResult> TrackAsync(TrackApplyClickRequest request, CancellationToken ct);

    /// <summary>AI-driven apply: resolves sevaone-rag's free-text service name against this
    /// catalog, logs the click (source=ai) either way, and mints a real apply URL via the same
    /// path the catalogue flow uses when a match is found. See TrackAiApplyClickResult's own
    /// doc comment for the no-match case.</summary>
    Task<TrackAiApplyClickResult> ResolveAndTrackAiApplyAsync(TrackAiApplyClickRequest request, CancellationToken ct);
}
