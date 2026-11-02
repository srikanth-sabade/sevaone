using SevaOne.Business.Stages.Packages;
using SevaOne.Business.Stages.Runtime;

namespace SevaOne.Business.Stages.Service;

/// <summary>
/// Orchestrates the pure <see cref="StageEngine"/>: load the case, load the exact package version
/// it is pinned to, run one step, and save the new state together with its events. Every write is
/// guarded by the case's revision, so two officers (or a callback racing a retry) cannot both win.
///
/// Not registered in dependency injection and not reachable over HTTP yet -- this is the spike
/// boundary. The controller, the Postgres stores and the outbox wiring are the next increment.
/// </summary>
public sealed class StageCaseService
{
    private readonly ICaseStore _cases;
    private readonly IPackageStore _packages;
    private readonly StageEngine _engine;
    private readonly TimeProvider _clock;

    public StageCaseService(ICaseStore cases, IPackageStore packages, StageEngine engine, TimeProvider clock)
    {
        _cases = cases;
        _packages = packages;
        _engine = engine;
        _clock = clock;
    }

    /// <summary>Starts a new case on the latest published version of the service.</summary>
    public async Task<CaseInstance> StartAsync(string serviceCode, string applicantId, CancellationToken ct = default)
    {
        var package = _packages.Latest(serviceCode);
        var started = _engine.Start(package, Guid.NewGuid(), applicantId, _clock.GetUtcNow());
        await _cases.CreateAsync(started.Case, started.Events, ct);
        return started.Case;
    }

    public Task<CaseInstance> SubmitAsync(Guid caseId, string formJson, CancellationToken ct = default) =>
        ApplyAsync(caseId, (package, c, now) => _engine.Submit(package, c, formJson, now), ct);

    public Task<CaseInstance> RecordPaymentAsync(
        Guid caseId, string feeReference, PaymentOutcome outcome, string paymentReference, CancellationToken ct = default) =>
        ApplyAsync(caseId, (package, c, now) => _engine.RecordPayment(package, c, feeReference, outcome, paymentReference, now), ct);

    public Task<CaseInstance> RetryFeeAsync(Guid caseId, CancellationToken ct = default) =>
        ApplyAsync(caseId, (package, c, now) => _engine.RetryFee(package, c, now), ct);

    public Task<CaseInstance> DecideAsync(
        Guid caseId, Decision decision, string officerId, IReadOnlyCollection<string> officerRoles, string? note,
        CancellationToken ct = default) =>
        ApplyAsync(caseId, (package, c, now) => _engine.Decide(package, c, decision, officerId, officerRoles, note, now), ct);

    /// <summary>Called by a timer; does nothing unless the applicant's window has lapsed.</summary>
    public Task<CaseInstance> ExpireAsync(Guid caseId, CancellationToken ct = default) =>
        ApplyAsync(caseId, (package, c, now) => _engine.Expire(package, c, now), ct);

    private async Task<CaseInstance> ApplyAsync(
        Guid caseId, Func<ServicePackage, CaseInstance, DateTimeOffset, Transition> step, CancellationToken ct)
    {
        var current = await _cases.GetAsync(caseId, ct)
            ?? throw new KeyNotFoundException($"Case {caseId} was not found.");

        // Always the version the case started on -- never "latest".
        var package = _packages.Get(current.ServiceCode, current.PackageVersion);
        var transition = step(package, current, _clock.GetUtcNow());

        if (ReferenceEquals(transition.Case, current))
        {
            if (transition.Events.Count > 0)
            {
                await _cases.AppendEventsAsync(transition.Events, ct);
            }

            return current;
        }

        await _cases.SaveAsync(transition.Case, current.Revision, transition.Events, ct);
        return transition.Case;
    }
}
