using System.Data;
using Dapper;

namespace SevaOne.Infrastructure.Repositories.Common;

/// <summary>Dapper predates System.DateOnly (added in .NET 6) and has no built-in DbType mapping for it — without this handler, passing a DateOnly? parameter (e.g. Citizen.DateOfBirth) throws NotSupportedException.</summary>
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value) => DateOnly.FromDateTime((DateTime)value);
}
