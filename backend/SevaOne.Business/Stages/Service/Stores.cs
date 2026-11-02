using SevaOne.Business.Stages.Packages;
using SevaOne.Business.Stages.Runtime;

namespace SevaOne.Business.Stages.Service;

/// <summary>Raised when a case was changed by someone else between being read and being saved.</summary>
public sealed class ConcurrencyException : Exception
{
    public ConcurrencyException(Guid caseId, int expectedRevision, int actualRevision)
        : base($"Case {caseId} is at revision {actualRevision}, not the expected {expectedRevision}.")
    {
    }
}

/// <summary>Published packages. A (ServiceCode, Version) pair is immutable once published.</summary>
public interface IPackageStore
{
    void Publish(ServicePackage package);

    ServicePackage Get(string serviceCode, int version);

    /// <summary>The highest published version -- what a brand-new case starts on.</summary>
    ServicePackage Latest(string serviceCode);
}

/// <summary>
/// Case storage. A real implementation writes the case row and its events in one Postgres
/// transaction, with <c>UPDATE ... WHERE revision = @expected</c> for <see cref="SaveAsync"/>.
/// </summary>
public interface ICaseStore
{
    Task<CaseInstance?> GetAsync(Guid id, CancellationToken ct = default);

    Task CreateAsync(CaseInstance created, IReadOnlyList<EngineEvent> events, CancellationToken ct = default);

    /// <exception cref="ConcurrencyException">The stored revision is not <paramref name="expectedRevision"/>.</exception>
    Task SaveAsync(CaseInstance updated, int expectedRevision, IReadOnlyList<EngineEvent> events, CancellationToken ct = default);

    /// <summary>Events that did not change state (a duplicate or orphaned payment) still need to reach the outbox.</summary>
    Task AppendEventsAsync(IReadOnlyList<EngineEvent> events, CancellationToken ct = default);
}

public sealed class InMemoryPackageStore : IPackageStore
{
    private readonly object _gate = new();
    private readonly Dictionary<(string Code, int Version), ServicePackage> _packages = [];

    public void Publish(ServicePackage package)
    {
        var errors = ServicePackageParser.Validate(package);
        if (errors.Count > 0)
        {
            throw new PackageValidationException(errors);
        }

        lock (_gate)
        {
            if (!_packages.TryAdd((package.ServiceCode, package.Version), package))
            {
                throw new InvalidOperationException($"{package.ServiceCode} v{package.Version} is already published and cannot be changed; publish a new version.");
            }
        }
    }

    public ServicePackage Get(string serviceCode, int version)
    {
        lock (_gate)
        {
            return _packages.TryGetValue((serviceCode, version), out var package)
                ? package
                : throw new KeyNotFoundException($"{serviceCode} v{version} is not published.");
        }
    }

    public ServicePackage Latest(string serviceCode)
    {
        lock (_gate)
        {
            var versions = _packages.Keys.Where(k => k.Code == serviceCode).Select(k => k.Version).ToList();
            return versions.Count == 0
                ? throw new KeyNotFoundException($"{serviceCode} has no published version.")
                : _packages[(serviceCode, versions.Max())];
        }
    }
}

public sealed class InMemoryCaseStore : ICaseStore
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, CaseInstance> _cases = [];
    private readonly List<EngineEvent> _outbox = [];

    /// <summary>Everything the engine has emitted so far, in order -- stands in for the outbox table.</summary>
    public IReadOnlyList<EngineEvent> Outbox
    {
        get
        {
            lock (_gate)
            {
                return _outbox.ToList();
            }
        }
    }

    public Task<CaseInstance?> GetAsync(Guid id, CancellationToken ct = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_cases.GetValueOrDefault(id));
        }
    }

    public Task CreateAsync(CaseInstance created, IReadOnlyList<EngineEvent> events, CancellationToken ct = default)
    {
        lock (_gate)
        {
            if (!_cases.TryAdd(created.Id, created))
            {
                throw new InvalidOperationException($"Case {created.Id} already exists.");
            }

            _outbox.AddRange(events);
        }

        return Task.CompletedTask;
    }

    public Task SaveAsync(CaseInstance updated, int expectedRevision, IReadOnlyList<EngineEvent> events, CancellationToken ct = default)
    {
        lock (_gate)
        {
            var stored = _cases.GetValueOrDefault(updated.Id)
                ?? throw new KeyNotFoundException($"Case {updated.Id} was not found.");
            if (stored.Revision != expectedRevision)
            {
                throw new ConcurrencyException(updated.Id, expectedRevision, stored.Revision);
            }

            _cases[updated.Id] = updated;
            _outbox.AddRange(events);
        }

        return Task.CompletedTask;
    }

    public Task AppendEventsAsync(IReadOnlyList<EngineEvent> events, CancellationToken ct = default)
    {
        lock (_gate)
        {
            _outbox.AddRange(events);
        }

        return Task.CompletedTask;
    }
}
