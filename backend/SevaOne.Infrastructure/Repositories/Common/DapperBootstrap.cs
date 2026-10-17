using System.Runtime.CompilerServices;
using Dapper;
using SevaOne.Domain.Enums;

namespace SevaOne.Infrastructure.Repositories.Common;

internal static class DapperBootstrap
{
    /// <summary>Lets every repository write plain `snake_case` SQL against `PascalCase` Domain POCOs without per-column aliasing, and maps the varchar-backed enums (operator_type, txn_status, payment_status) that Dapper can't bind automatically. ApplicationStatus stays a plain int column, which Dapper already handles.</summary>
    [ModuleInitializer]
#pragma warning disable CA2255 // deliberate: guarantees Dapper's snake_case mapping is active regardless of app composition root
    internal static void Init()
#pragma warning restore CA2255
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SqlMapper.AddTypeHandler(new EnumStringTypeHandler<OperatorType>());
        SqlMapper.AddTypeHandler(new EnumStringTypeHandler<WalletTxnStatus>());
        SqlMapper.AddTypeHandler(new EnumStringTypeHandler<PaymentPostStatus>());
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
    }
}
