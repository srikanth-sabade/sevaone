using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace SevaOne.Domain.Connection;

public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SevaOneDb")
            ?? throw new InvalidOperationException("Connection string 'SevaOneDb' not configured.");
    }

    public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
}
