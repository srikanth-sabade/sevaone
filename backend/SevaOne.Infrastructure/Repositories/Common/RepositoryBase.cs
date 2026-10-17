using System.Data;
using System.Data.Common;
using SevaOne.Domain.Connection;

namespace SevaOne.Infrastructure.Repositories.Common;

/// <summary>
/// Every repository call either joins an in-flight <see cref="IUnitOfWork"/> (so several
/// repositories can share one transaction) or opens and disposes its own short-lived
/// connection. There is no Dapper change tracker to fall back on, so this is the only place
/// that connection lifetime is decided.
/// </summary>
public abstract class RepositoryBase
{
    private readonly IDbConnectionFactory _factory;

    protected RepositoryBase(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    protected async Task<(DbConnection Connection, IDbTransaction? Transaction, bool Owned)> ResolveAsync(IUnitOfWork? uow, CancellationToken ct)
    {
        if (uow is not null)
        {
            return ((DbConnection)uow.Connection, uow.Transaction, false);
        }

        var connection = (DbConnection)_factory.CreateConnection();
        await connection.OpenAsync(ct);
        return (connection, null, true);
    }
}
