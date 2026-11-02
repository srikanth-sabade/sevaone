using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SevaOne.Business.Common;
using SevaOne.Business.Discovery.DTOs;
using SevaOne.Business.Discovery.Interfaces;
using SevaOne.Domain.Enums;
using SevaOne.Domain.Models;
using SevaOne.Infrastructure.Repositories.Audit;
using SevaOne.Infrastructure.Repositories.Discovery;
using SevaOne.Infrastructure.Repositories.Notification;
using SevaOne.Infrastructure.Repositories.Orchestration;
using SevaOne.Infrastructure.Repositories.Portal;
using SevaOne.Infrastructure.Repositories.Status;
using SevaOne.Infrastructure.Security;

namespace SevaOne.Business.Discovery.Services;

/// <summary>
/// Acknowledgement API of the token integration (Seva Sindhu V3, URL 2). After the department has the
/// application it posts the result here; we check who is calling and that the application is theirs,
/// record the call, and put the application on the citizen's dashboard (the same applications and status
/// events the rest of SevaOne uses), so no separate dashboard is needed.
/// </summary>
public sealed partial class TokenAcknowledgementService : ITokenAcknowledgementService
{
    private const int MaxFileBytes = 6 * 1024 * 1024;
    private static readonly TimeSpan IndianStandardTime = TimeSpan.FromHours(5.5);

    private readonly ITokenIntegrationRepository _integrations;
    private readonly IOrchestrationRepository _orchestration;
    private readonly IStatusRepository _status;
    private readonly TokenApplicationWriter _writer;
    private readonly IApplyRedirectProtector _protector;
    private readonly IClock _clock;
    private readonly ICurrentCitizen _current;

    public TokenAcknowledgementService(
        ITokenIntegrationRepository integrations, IOrchestrationRepository orchestration, IStatusRepository status,
        TokenApplicationWriter writer, IApplyRedirectProtector protector, IClock clock, ICurrentCitizen current)
    {
        _integrations = integrations;
        _orchestration = orchestration;
        _status = status;
        _writer = writer;
        _protector = protector;
        _clock = clock;
        _current = current;
    }

    public async Task<TokenIntegrationCaller?> AuthenticateAsync(string? authorizationHeader, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader) || !authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return null;

        string decoded;
        try { decoded = Encoding.UTF8.GetString(Convert.FromBase64String(authorizationHeader[6..].Trim())); }
        catch (FormatException) { return null; }

        var colon = decoded.IndexOf(':');
        if (colon <= 0) return null;
        var username = decoded[..colon];
        var password = decoded[(colon + 1)..];

        var credential = await _integrations.GetCredentialAsync(username, ct);
        // Same work whether or not the username exists, so a caller cannot tell the two apart by timing.
        var passwordOk = TokenIntegrationSecrets.Verify(password, credential?.PasswordHash ?? TokenIntegrationSecrets.DummyHash);
        if (credential is null || !passwordOk || !credential.IsActive) return null;

        await _integrations.TouchCredentialAsync(credential.Id, ct);
        return new TokenIntegrationCaller(credential.Id, credential.DepartmentId, credential.DepartmentCode);
    }

    public async Task<TokenIntegrationFileDto?> GetAcknowledgementFileAsync(string reference, CancellationToken ct)
    {
        var citizenId = _current.CitizenId ?? throw new SevaOne.Domain.Common.ForbiddenException("Not authenticated.");
        var file = await _integrations.GetAcknowledgementFileAsync(reference, citizenId, ct);
        if (file is null || file.Content.Length == 0) return null;
        var (contentType, extension) = file.FileType switch
        {
            "pdf" => ("application/pdf", "pdf"),
            "png" => ("image/png", "png"),
            _ => ("image/jpeg", "jpg"),
        };
        return new TokenIntegrationFileDto(contentType, $"Acknowledgement-{file.FileName}.{extension}", file.Content);
    }

    public async Task<TokenAckOutcome> AcknowledgeAsync(TokenAckRequest request, TokenIntegrationCaller caller, string? callerIp, CancellationToken ct)
    {
        var edcsRef = request.EdcsApplicationRefNo?.Trim();
        var deptRef = request.DepartmentApplicationRefNo?.Trim();
        if (string.IsNullOrEmpty(edcsRef) || edcsRef.Length > 32) return Fail("EdcsApplicationRefNo missing or too long");
        if (string.IsNullOrEmpty(deptRef) || deptRef.Length > 100) return Fail("DepartmentApplicationRefNo missing or too long");

        var attempt = await _integrations.GetAttemptAsync(edcsRef, ct);
        if (attempt is null) return Fail($"unknown application reference {edcsRef}");
        if (attempt.DepartmentId != caller.DepartmentId) return Fail($"{edcsRef} belongs to a different department than login {caller.DepartmentCode}");
        if (!attempt.IsSandbox && attempt.AllowedIps.Length > 0 && !IpAllowed(callerIp, attempt.AllowedIps)) return Fail($"caller address {callerIp ?? "(unknown)"} is not on the allowed list for this service");

        if (!string.IsNullOrWhiteSpace(request.UserId) && request.UserId.Trim() != attempt.SevaOneUserId.ToString(CultureInfo.InvariantCulture))
            return Fail("UserId does not match the user this application was started for");

        // The token is the proof the department received from us. It may be echoed back long after its
        // 30 minutes (the user fills a form first), so only identity is checked, not expiry.
        bool? tokenVerified = null;
        if (!string.IsNullOrWhiteSpace(request.Token))
        {
            var plain = _protector.TryUnprotect(request.Token.Trim());
            tokenVerified = plain is not null && attempt.TokenHash is not null
                && string.Equals(Sha256Hex(plain), attempt.TokenHash, StringComparison.OrdinalIgnoreCase);
            if (tokenVerified == false) return Fail("Token does not match the one issued for this application");
        }

        if (request.DepartmentFee is < 0) return Fail("DepartmentFee cannot be negative");
        var feeDate = ParseDate(request.DepartmentFeeCollectionDate, out var feeDateOk);
        if (!feeDateOk) return Fail("DepartmentFeeCollectionDate is not a valid date");
        var ackDate = ParseDate(request.AcknowledgmentDate, out var ackDateOk);
        if (!ackDateOk) return Fail("AcknowledgmentDate is not a valid date");

        var fileType = NormalizeFileType(request.AcknowledgmentFileType);
        byte[]? file = null;
        if (!string.IsNullOrWhiteSpace(request.Acknowledgment))
        {
            if (fileType is null) return Fail("AcknowledgmentFileType must be jpeg, png or pdf");
            var base64 = StripDataUri(request.Acknowledgment.Trim());
            if (base64.Length / 4 * 3 > MaxFileBytes + 4) return Fail("Acknowledgment file is too large");
            try { file = Convert.FromBase64String(base64); }
            catch (FormatException) { return Fail("Acknowledgment is not valid base64"); }
            if (file.Length is 0 or > MaxFileBytes) return Fail("Acknowledgment file is empty or too large");
            if (!MatchesFileType(file, fileType)) return Fail("Acknowledgment content does not match AcknowledgmentFileType");
        }

        var paymentStatus = request.DepartmentPaymentStatus?.Trim();
        if (paymentStatus is { Length: > 50 }) return Fail("DepartmentPaymentStatus is too long");
        var paymentRef = request.DepartmentPaymentRefNumber?.Trim();
        if (paymentRef is { Length: > 100 }) return Fail("DepartmentPaymentRefNumber is too long");

        // What we keep of the call: everything except the file itself (stored on the application) and the token.
        var payload = JsonSerializer.Serialize(new
        {
            request.UserId,
            DepartmentApplicationRefNo = deptRef,
            EdcsApplicationRefNo = edcsRef,
            DepartmentPaymentStatus = paymentStatus,
            request.DepartmentFeeCollectionDate,
            request.DepartmentFee,
            DepartmentPaymentRefNumber = paymentRef,
            AcknowledgmentFileType = fileType,
            request.AcknowledgmentDate,
            AcknowledgmentBytes = file?.Length,
            AcknowledgmentSha256 = file is null ? null : Convert.ToHexString(SHA256.HashData(file)).ToLowerInvariant(),
        });

        var result = await _integrations.ApplyAcknowledgementAsync(new TokenIntegrationAckWrite
        {
            AttemptId = attempt.AttemptId,
            DepartmentApplicationRefNo = deptRef,
            PaymentStatus = paymentStatus,
            Fee = request.DepartmentFee,
            FeeCollectionDate = feeDate,
            PaymentRefNumber = paymentRef,
            FileType = fileType,
            AcknowledgmentDate = ackDate,
            Acknowledgment = file,
            Payload = payload,
            PayloadHash = Sha256Hex(payload),
            CallerIp = callerIp,
            TokenVerified = tokenVerified,
        }, ct);

        // A practice (sandbox) acknowledgement is recorded like any other, but never becomes an application on a dashboard.
        if (!attempt.IsSandbox) await EnsureApplicationAsync(attempt, deptRef, paymentStatus, request.DepartmentFee, ackDate ?? feeDate, result.AlreadyReceived, callerIp, ct);

        return new TokenAckOutcome(new TokenAckSuccess(result.UpdateId, edcsRef, deptRef), null);
    }

    /// <summary>Puts the application on the citizen's dashboard the first time, and adds a status event
    /// when a later acknowledgement changes something. Safe to run again for the same call.</summary>
    private async Task EnsureApplicationAsync(
        TokenIntegrationAttemptRow attempt, string deptRef, string? paymentStatus, decimal? fee, DateTimeOffset? submittedAt,
        bool alreadyReceived, string? callerIp, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var summary = DescribeUpdate(deptRef, paymentStatus, fee);
        var existing = await _orchestration.GetApplicationByReferenceAsync(attempt.EdcsApplicationRefNo, ct);

        if (existing is not null)
        {
            if (!alreadyReceived)
            {
                await _status.InsertApplicationEventAsync(new ApplicationEvent
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = existing.Id,
                    Status = existing.Status,
                    Note = $"Update from the department. {summary}",
                    At = now,
                }, ct);
            }
            return;
        }

        await _writer.CreateAsync(new TokenApplicationNew(
            attempt.CitizenId, attempt.ServiceId, attempt.DepartmentId, attempt.DepartmentCode, attempt.ServiceName, attempt.EdcsApplicationRefNo,
            deptRef, ApplicationStatus.Submitted, submittedAt ?? now, attempt.SakalaDays,
            $"Application submitted on the department's portal. {summary}", "application.token_integration.acknowledged", callerIp), ct);
    }

    private static string DescribeUpdate(string deptRef, string? paymentStatus, decimal? fee)
    {
        var parts = new List<string> { $"Department reference {deptRef}." };
        if (!string.IsNullOrWhiteSpace(paymentStatus))
            parts.Add(fee is { } f ? $"Payment {paymentStatus}, Rs {f.ToString("0.##", CultureInfo.InvariantCulture)}." : $"Payment {paymentStatus}.");
        return string.Join(' ', parts);
    }

    private static TokenAckOutcome Fail(string reason) => new(null, reason);

    private static string Sha256Hex(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    [GeneratedRegex(@"(Z|[+-]\d{2}:?\d{2})$", RegexOptions.IgnoreCase)]
    private static partial Regex HasOffset();

    /// <summary>The contract sends times like 2024-08-17T10:30:00 with no zone: read as Indian Standard Time, kept as UTC (the database only takes UTC).</summary>
    private static DateTimeOffset? ParseDate(string? value, out bool ok)
    {
        ok = true;
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = value.Trim();
        if (HasOffset().IsMatch(text))
        {
            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var withOffset)) return withOffset.ToUniversalTime();
        }
        else if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
        {
            return new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), IndianStandardTime).ToUniversalTime();
        }
        ok = false;
        return null;
    }

    private static string? NormalizeFileType(string? value)
    {
        var type = value?.Trim().TrimStart('.').ToLowerInvariant();
        return type switch { "jpg" or "jpeg" => "jpeg", "png" => "png", "pdf" => "pdf", _ => null };
    }

    private static string StripDataUri(string value)
    {
        var comma = value.IndexOf(',');
        return value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0 ? value[(comma + 1)..] : value;
    }

    /// <summary>The file is shown to the citizen, so its bytes must really be what the department says they are.</summary>
    private static bool MatchesFileType(byte[] file, string type) => type switch
    {
        "jpeg" => file.Length > 3 && file[0] == 0xFF && file[1] == 0xD8 && file[2] == 0xFF,
        "png" => file.Length > 8 && file[0] == 0x89 && file[1] == 0x50 && file[2] == 0x4E && file[3] == 0x47,
        "pdf" => file.Length > 4 && file[0] == 0x25 && file[1] == 0x50 && file[2] == 0x44 && file[3] == 0x46,
        _ => false,
    };

    private static bool IpAllowed(string? callerIp, IEnumerable<string> allowed)
    {
        if (!IPAddress.TryParse(callerIp, out var ip)) return false;
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        foreach (var entry in allowed)
        {
            if (entry.Contains('/'))
            {
                if (IPNetwork.TryParse(entry, out var network) && network.Contains(ip)) return true;
            }
            else if (IPAddress.TryParse(entry, out var single) && (single.IsIPv4MappedToIPv6 ? single.MapToIPv4() : single).Equals(ip))
            {
                return true;
            }
        }
        return false;
    }
}
