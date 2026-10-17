using System.Data;
using System.Data.Common;
using SevaOne.Domain.Connection;

namespace SevaOne.Infrastructure.Repositories.Common;

public sealed class UnitOfWork : IUnitOfWork
{
    private bool _completed;

    private UnitOfWork(DbConnection connection, DbTransaction transaction)
    {
        Connection = connection;
        Transaction = transaction;
    }

    public IDbConnection Connection { get; }
    public IDbTransaction Transaction { get; }

    public static async Task<UnitOfWork> BeginAsync(IDbConnectionFactory factory, CancellationToken ct)
    {
        var connection = (DbConnection)factory.CreateConnection();
        await connection.OpenAsync(ct);
        var transaction = await connection.BeginTransactionAsync(ct);
        return new UnitOfWork(connection, transaction);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        await ((DbTransaction)Transaction).CommitAsync(ct);
        _completed = true;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        await ((DbTransaction)Transaction).RollbackAsync(ct);
        _completed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            await ((DbTransaction)Transaction).RollbackAsync();
        }

        await ((DbTransaction)Transaction).DisposeAsync();
        await ((DbConnection)Connection).DisposeAsync();
    }
}

public sealed class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IDbConnectionFactory _factory;

    public UnitOfWorkFactory(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IUnitOfWork> BeginAsync(CancellationToken ct = default) => await UnitOfWork.BeginAsync(_factory, ct);
}
