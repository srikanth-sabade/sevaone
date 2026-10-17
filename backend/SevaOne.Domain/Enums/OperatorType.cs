namespace SevaOne.Domain.Enums;

/// <summary>Assisted-mode operator category: G1/K1/B1 kiosk operator classes.</summary>
public enum OperatorType
{
    G1 = 0,
    K1 = 1,
    B1 = 2,

    /// <summary>Self-claimed from portal.operator_whitelist, whose kiosk_type ("SPEW") doesn't
    /// map to G1/K1/B1 (Grama One/Karnataka One/Bangalore One) -- unmapped rather than guessed
    /// until the real mapping is known. See V56__operator_master_claim_support.sql.</summary>
    UNK = 3
}
