using SevaOne.Domain.Common;
using SevaOne.Domain.Enums;

namespace SevaOne.Domain.Models;

/// <summary>operator_master — G1/K1/B1 kiosk operators who run Assisted Mode on behalf of citizens.</summary>
public sealed class OperatorMaster : BaseModel
{
    /// <summary>Business operator id ("OPERATOR_ID" in the flow) — unique, e.g. a G1/K1/B1 kiosk code.</summary>
    public string OperatorCode { get; set; } = string.Empty;

    public OperatorType OperatorType { get; set; }
    public string? OperatorName { get; set; }
    public string? MobileNo { get; set; }

    /// <summary>Set when the self-claim flow verified this operator's mobile via OTP (see
    /// OperatorClaimService). Null for pre-seeded operators that never went through claim.</summary>
    public DateTimeOffset? MobileVerifiedAt { get; set; }

    /// <summary>Identifier for this operator's wallet on the external wallet provider — SevaOne calls out to that system (balance, other wallet info) keyed on this. Null until the operator is provisioned there.</summary>
    public string? WalletId { get; set; }

    /// <summary>WSO2 IS SCIM2 username ("sevaone-operator-{whitelistUserId}") — the real credential
    /// for operators claimed via OperatorClaimService. Null for pre-seeded legacy operators
    /// (e.g. R__seed_operator.sql's OPR10001), which still log in via PasswordHash until
    /// migrated — see OperatorAuthService.LoginAsync.</summary>
    public string? Wso2Subject { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}
