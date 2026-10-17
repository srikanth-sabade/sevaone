namespace SevaOne.Domain.Common;

/// <summary>
/// Stable, documented error codes returned in the `code` field of department-facing error
/// responses (scope: /api/v1/dept/* only -- see backend/docs/ERROR_CODES.md for the full
/// catalog, the three-layer error model, and why citizen-facing endpoints don't have this yet).
///
/// Format: SEVA-&lt;AREA&gt;-&lt;NNNN&gt;. Once a code ships, it's permanent -- a department may already
/// be branching on it in production code. Add new codes, never renumber or reuse an old one,
/// even if the underlying condition is later removed.
/// </summary>
public static class SevaErrorCodes
{
    /// <summary>Cross-cutting request-shape failures, checked before any endpoint-specific business logic runs.</summary>
    public static class Request
    {
        public const string ValidationFailed = "SEVA-REQUEST-4001";
    }

    /// <summary>DeptGatewayAuthMiddleware -- proves the request genuinely passed through APIM.</summary>
    public static class Gateway
    {
        public const string MissingOrInvalidSecret = "SEVA-GATEWAY-4001";
    }

    /// <summary>DeptMachineAuth -- proves WHICH department is calling, independent of APIM.</summary>
    public static class Auth
    {
        public const string MissingCredentials = "SEVA-AUTH-4001";
        public const string InvalidCredentials = "SEVA-AUTH-4002";
        public const string DepartmentMismatch = "SEVA-AUTH-4003";
        public const string NotConfigured = "SEVA-AUTH-4004";
    }

    /// <summary>PortalDeptFacingService -- token/business-logic failures once both auth layers passed.</summary>
    public static class Dept
    {
        public const string TokenInvalid = "SEVA-DEPT-4001";
        public const string TokenExpired = "SEVA-DEPT-4002";
        public const string TokenAlreadyUsed = "SEVA-DEPT-4003";
        public const string CitizenNotFound = "SEVA-DEPT-4004";
        public const string SchemeNotFound = "SEVA-DEPT-4005";
        public const string DepartmentNotFound = "SEVA-DEPT-4006";
        public const string UnknownPaymentStatus = "SEVA-DEPT-4007";
        public const string PaymentWindowMissing = "SEVA-DEPT-4008";
        public const string PaymentWindowExpired = "SEVA-DEPT-4009";
        public const string ApplicationNotFoundForReference = "SEVA-DEPT-4010";
    }
}
