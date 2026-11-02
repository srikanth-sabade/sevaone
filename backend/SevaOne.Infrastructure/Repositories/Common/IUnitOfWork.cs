using System.Data;

namespace SevaOne.Infrastructure.Repositories.Common;

/// <summary>
/// Wraps one open connection + transaction so a business feature that must touch more than
/// one repository atomically (e.g. wallet deduct + ledger insert + token issue) can pass the
/// same pair into each repository call. Dapper has no change tracker to coordinate this
/// automatically, so the transaction boundary is explicit and owned by the caller.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IDbConnection Connection { get; }
    IDbTransaction Transaction { get; }
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}

public interface IUnitOfWorkFactory
{
    Task<IUnitOfWork> BeginAsync(CancellationToken ct = default);
}
