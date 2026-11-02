using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SevaOne.Infrastructure.ExternalAuth;

/// <summary>
/// Sends the mobile+OTP login flow's OTP. Deliberately a thin, swappable interface (same
/// pattern as IDigiLockerClient) -- no SMS provider is chosen yet for this project. Register a
/// real implementation (MSG91/Kaleyra/a state-government gateway, etc.) via DI when one is
/// available; LoggedSmsOtpSender below is the local-dev stand-in until then, matching how
/// PortalAuthService.SendOtpEmailAsync already just needs MailHog for the Email OTP path.
/// </summary>
public interface ISmsOtpSender
{
    Task SendOtpAsync(string mobile, string otp, CancellationToken ct);
}

/// <summary>No real SMS provider configured -- logs the OTP instead of sending it, for local dev/testing only. Swap the DI registration for a real ISmsOtpSender before this goes anywhere near production.</summary>
public sealed class LoggedSmsOtpSender : ISmsOtpSender
{
    private readonly ILogger<LoggedSmsOtpSender> _log;
    private readonly DevOtpInbox _inbox;

    public LoggedSmsOtpSender(ILogger<LoggedSmsOtpSender> log, DevOtpInbox inbox)
    {
        _log = log;
        _inbox = inbox;
    }

    public Task SendOtpAsync(string mobile, string otp, CancellationToken ct)
    {
        _log.LogWarning(
            "No ISmsOtpSender configured -- SMS OTP for {Mobile}: {Otp} (logged only, not actually sent)",
            mobile, otp);
        _inbox.Record(mobile, otp);
        return Task.CompletedTask;
    }
}

public sealed record DevOtpEntry(string Mobile, string Otp, DateTimeOffset SentAt);

/// <summary>
/// Dev-only in-memory OTP log, fed by LoggedSmsOtpSender, so local testing can read the last few
/// OTPs from a browser tab (GET /dev/otp-inbox) instead of grepping the console/log file. Never
/// wire this into a real ISmsOtpSender implementation -- it exists only because no real SMS
/// provider is configured yet.
/// </summary>
public sealed class DevOtpInbox
{
    private const int Capacity = 25;
    private readonly ConcurrentQueue<DevOtpEntry> _entries = new();

    public void Record(string mobile, string otp)
    {
        _entries.Enqueue(new DevOtpEntry(mobile, otp, DateTimeOffset.UtcNow));
        while (_entries.Count > Capacity && _entries.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyList<DevOtpEntry> GetRecent() => _entries.Reverse().ToList();
}
