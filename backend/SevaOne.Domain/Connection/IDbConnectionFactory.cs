using System.Data;

namespace SevaOne.Domain.Connection;

/// <summary>Hands repositories an open connection. Dapper has no unit-of-work/change-tracker — each repository call opens (or is given) a connection and runs SQL directly.</summary>
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
