using Microsoft.AspNetCore.Mvc;
using SevaOne.Infrastructure.ExternalAuth;

namespace SevaOne.Services.Controllers;

/// <summary>
/// Dev-only conveniences that must never be reachable outside Development -- both actions 404
/// in any other environment, checked first thing in each handler rather than relying solely on
/// routing/hosting config to keep this off in prod.
/// </summary>
[Route("")]
public sealed class DevController : SevaControllerBase
{
    private readonly DevOtpInbox _inbox;
    private readonly IHostEnvironment _env;

    public DevController(DevOtpInbox inbox, IHostEnvironment env)
    {
        _inbox = inbox;
        _env = env;
    }

    [HttpGet("api/v1/dev/otp-inbox")]
    public IActionResult OtpInboxJson()
    {
        if (!_env.IsDevelopment()) return NotFound();
        return Ok(_inbox.GetRecent());
    }

    [HttpGet("dev/otp-inbox")]
    public IActionResult OtpInboxPage()
    {
        if (!_env.IsDevelopment()) return NotFound();
        return Content(PageHtml, "text/html");
    }

    // The app's CSP is script-src 'self' with no 'unsafe-inline', so the polling logic has to
    // ship as its own same-origin file rather than an inline <script> block -- an inline script
    // here would just get silently dropped by the browser, no visible error.
    [HttpGet("dev/otp-inbox.js")]
    public IActionResult OtpInboxScript()
    {
        if (!_env.IsDevelopment()) return NotFound();
        return Content(PageJs, "application/javascript");
    }

    private const string PageHtml = """
        <!doctype html>
        <html>
        <head>
        <meta charset="utf-8">
        <title>SevaOne dev — OTP inbox</title>
        <style>
          body { font-family: system-ui, sans-serif; background: #0f172a; color: #e2e8f0; margin: 0; padding: 2rem; }
          h1 { font-size: 1.1rem; color: #94a3b8; font-weight: 600; }
          table { width: 100%; border-collapse: collapse; margin-top: 1rem; }
          th, td { text-align: left; padding: 0.5rem 0.75rem; border-bottom: 1px solid #1e293b; }
          th { color: #64748b; font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.05em; }
          td.otp { font-family: ui-monospace, monospace; font-size: 1.1rem; font-weight: 700; color: #4ade80; letter-spacing: 0.1em; }
          td.mobile { font-family: ui-monospace, monospace; color: #e2e8f0; }
          td.age { color: #64748b; font-size: 0.85rem; }
          tr:first-child td.otp { color: #86efac; text-shadow: 0 0 8px rgba(74,222,128,.5); }
          .empty { color: #475569; margin-top: 1rem; }
        </style>
        </head>
        <body>
        <h1>SevaOne dev — OTP inbox (local only, auto-refreshes every 2s)</h1>
        <table id="t"><thead><tr><th>Mobile</th><th>OTP</th><th>Sent</th></tr></thead><tbody></tbody></table>
        <p class="empty" id="empty" style="display:none">No OTPs sent yet — trigger a login to see one here.</p>
        <script src="/dev/otp-inbox.js"></script>
        </body>
        </html>
        """;

    private const string PageJs = """
        async function tick() {
          const res = await fetch('/api/v1/dev/otp-inbox');
          const rows = await res.json();
          const body = document.querySelector('#t tbody');
          const empty = document.querySelector('#empty');
          empty.style.display = rows.length ? 'none' : 'block';
          body.innerHTML = rows.map(r => {
            const secs = Math.max(0, Math.round((Date.now() - Date.parse(r.sentAt)) / 1000));
            return `<tr><td class="mobile">${r.mobile}</td><td class="otp">${r.otp}</td><td class="age">${secs}s ago</td></tr>`;
          }).join('');
        }
        tick();
        setInterval(tick, 2000);
        """;
}
