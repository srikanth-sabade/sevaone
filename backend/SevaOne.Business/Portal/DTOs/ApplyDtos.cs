namespace SevaOne.Business.Portal.DTOs;

/// <summary>DepartmentCode is required -- service_id is only unique within a department (see
/// V39), so minting against the wrong department's same-numbered service is a real
/// misrouting risk otherwise, not just a theoretical one.</summary>
public sealed record CreateApplyTokenRequest(string DepartmentCode, string ServiceCode);

/// <summary>Operator-assisted apply, minted by a logged-in operator (ICurrentOperator) on behalf of a citizen identified by their public SevaOneUserID -- not the citizen's own session.</summary>
public sealed record CreateAssistedApplyTokenRequest(string DepartmentCode, string ServiceCode, long SevaOneUserId);

/// <summary>
/// Department integration testing only -- mints a token for one of the fixed seeded test
/// citizens (see R__seed_test_citizens.sql), bypassing citizen/operator login entirely so a
/// department can self-serve a fresh Token for their own /dept/user-details testing. DepartmentId
/// here is the authenticated caller (resolved by DeptController.TryResolveDepartment), used only
/// to check the requested ServiceCode actually belongs to them -- never trusted from raw body input.
/// </summary>
public sealed record CreateTestApplyTokenRequest(string DepartmentId, string ServiceCode);

/// <summary>ExpiresAt is null when IsHomepageFallback is true — no token was minted, so there's nothing with a real expiry (the department has no deep link configured for this audience yet, so the citizen/operator is just sent to the department's homepage).</summary>
public sealed record ApplyTokenDto(string ApplyUrl, DateTimeOffset? ExpiresAt, bool IsHomepageFallback = false);

/// <summary>Citizen self-service apply outcome. RequiresLogin=true means a real deep link exists
/// for this service but the caller isn't authenticated yet -- ApplyUrl/ExpiresAt are null in
/// that case; the frontend sends the citizen to log in, then calls this endpoint again. Never
/// true when IsHomepageFallback is true -- that case never needs identity at all, so it always
/// resolves on the first anonymous call.</summary>
public sealed record ApplyOutcomeDto(bool RequiresLogin, string? ApplyUrl, DateTimeOffset? ExpiresAt, bool IsHomepageFallback);

/// <summary>Public interest-measurement click -- deliberately separate from minting a real
/// token. AssistedMode+SevaOneUserId are only meaningful for an operator-assisted click on a
/// bound citizen (same client-trust pattern CreateAssistedApplyTokenRequest already uses);
/// otherwise identity is resolved from ICurrentCitizen and may be null (anonymous browsing).</summary>
public sealed record TrackApplyClickRequest(string DepartmentCode, string ServiceCode, bool AssistedMode = false, long? SevaOneUserId = null);

/// <summary>Tracked=false means the click was a silent no-op (department/service not found --
/// e.g. an AI-resolved service that doesn't map to a real catalog row yet). Never an error.</summary>
public sealed record TrackApplyClickResult(bool Tracked);

/// <summary>AI-driven apply request -- identifies the service by its display NAME, never a
/// code: sevaone-rag is a fully separate catalog whose own identifiers never line up with this
/// one, so name is the only reliable link between them. DepartmentCode is an optional
/// disambiguating hint (sevaone-rag's own best-guess mapping of its free-text department label
/// to a real department code, when it recognizes one) -- preferred when given, never required.
/// See ApplyClickTrackingService.ResolveAndTrackAiApplyAsync.</summary>
public sealed record TrackAiApplyClickRequest(string ServiceName, string? DepartmentCode = null, string? OperatorHint = null, bool AssistedMode = false, long? SevaOneUserId = null);

/// <summary>Matched=false means sevaone-rag resolved a service that doesn't exist in this
/// catalog yet -- the click is still logged (source=ai) for visibility, but with no real scheme
/// to mint an apply URL for; the frontend falls back to whatever URL the AI already had.
/// Matched=true carries the same shape as the catalogue apply flow's ApplyOutcomeDto, since a
/// match delegates to the exact same CreateApplyTokenAsync used there.</summary>
public sealed record TrackAiApplyClickResult(bool Matched, bool RequiresLogin = false, string? ApplyUrl = null, DateTimeOffset? ExpiresAt = null, bool IsHomepageFallback = false);

/// <summary>Result of resolving the department's return-redirect. ReturnTo is always a relative path -- the controller prefixes it with the frontend origin before redirecting.</summary>
public sealed record ApplyReturnResult(string ReturnTo, bool Success);
