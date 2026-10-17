using System.Data;
using Dapper;

namespace SevaOne.Infrastructure.Repositories.Common;

/// <summary>
/// Dapper maps int columns to enums natively but not varchar ones. operator_type,
/// txn_status and payment_status are stored as their C# enum name (varchar) rather than an
/// int, so each needs this handler registered once at startup.
/// </summary>
public sealed class EnumStringTypeHandler<TEnum> : SqlMapper.TypeHandler<TEnum>
    where TEnum : struct, Enum
{
    public override void SetValue(IDbDataParameter parameter, TEnum value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value.ToString();
    }

    public override TEnum Parse(object value) => Enum.Parse<TEnum>((string)value, ignoreCase: true);
}
