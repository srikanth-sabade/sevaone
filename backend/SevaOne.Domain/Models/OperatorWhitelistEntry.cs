namespace SevaOne.Domain.Models;

/// <summary>
/// One row of portal.operator_whitelist -- proof that a user_id is a real, department-registered
/// SevaSindhu operator. Not a credentials record: no password/name lives here, only what the
/// source roster export contained. See V54__operator_whitelist.sql.
/// </summary>
public sealed class OperatorWhitelistEntry
{
    public long UserId { get; set; }
    public string? RegistrationId { get; set; }
    public int? KioskTypeId { get; set; }
    public string? KioskType { get; set; }
    public short SpdiStatus { get; set; }
}
