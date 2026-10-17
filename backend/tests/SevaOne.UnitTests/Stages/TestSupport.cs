using SevaOne.Business.Stages.Packages;
using SevaOne.Business.Stages.Runtime;
using SevaOne.Business.Stages.Samples;

namespace SevaOne.UnitTests.Stages;

internal sealed class ManualClock : TimeProvider
{
    private DateTimeOffset _now;

    public ManualClock(DateTimeOffset start)
    {
        _now = start;
    }

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}

internal static class Pilot
{
    public static readonly DateTimeOffset T0 = new(2026, 10, 15, 9, 0, 0, TimeSpan.Zero);

    public static readonly string[] Tahsildar = ["tahsildar"];

    public static readonly string[] RevenueOfficer = ["revenue-officer"];

    public static ServicePackage Package() => PilotPackages.IncomeCertificate();

    /// <summary>The pilot, with a 5-day applicant window on stage 1 as well, to exercise expiry while a fee is pending.</summary>
    public static ServicePackage PackageWithStageOneExpiry()
    {
        var package = Package();
        return package with { Stages = [package.Stages[0] with { ExpiresAfterDays = 5 }, package.Stages[1]] };
    }
}

/// <summary>Drives one case through the engine and keeps every event it emitted, in order.</summary>
internal sealed class Scenario
{
    public Scenario(ServicePackage? package = null)
    {
        Package = package ?? Pilot.Package();
        Take(Engine.Start(Package, Guid.NewGuid(), "citizen-1", Now));
    }

    public StageEngine Engine { get; } = new();

    public ServicePackage Package { get; }

    public CaseInstance Case { get; private set; } = null!;

    public List<EngineEvent> Events { get; } = [];

    public DateTimeOffset Now { get; set; } = Pilot.T0;

    public Transition Submit(string form = "{}") => Take(Engine.Submit(Package, Case, form, Now));

    public Transition Pay(PaymentOutcome outcome = PaymentOutcome.Paid, string? feeReference = null, string paymentReference = "PAY-1") =>
        Take(Engine.RecordPayment(Package, Case, feeReference ?? Case.Current.FeeReference!, outcome, paymentReference, Now));

    public Transition Retry() => Take(Engine.RetryFee(Package, Case, Now));

    public Transition Decide(Decision decision, string[] roles, string? note = null, string officer = "officer-1") =>
        Take(Engine.Decide(Package, Case, decision, officer, roles, note, Now));

    public Transition Expire() => Take(Engine.Expire(Package, Case, Now));

    /// <summary>Stage 1 through to approval, leaving stage 2 open. Returns nothing; inspect <see cref="Case"/>.</summary>
    public void CompleteStageOne()
    {
        Submit();
        Pay();
        Decide(Decision.Approve, Pilot.Tahsildar);
    }

    private Transition Take(Transition transition)
    {
        Case = transition.Case;
        Events.AddRange(transition.Events);
        return transition;
    }
}
