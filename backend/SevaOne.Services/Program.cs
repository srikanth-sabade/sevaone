using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Serilog;
using SevaOne.Domain.Common;
using SevaOne.Business.Common;
using SevaOne.Infrastructure.ExternalAuth;
using SevaOne.Services.Auth;
using SevaOne.Services.Extensions;
using SevaOne.Services.HealthChecks;
using SevaOne.Services.Middleware;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

    builder.Services.AddSevaOneServices(builder.Configuration);
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentCitizen, HttpCurrentCitizen>();
    builder.Services.AddScoped<ICurrentOperator, HttpCurrentOperator>();
    builder.Services.AddScoped<ICurrentGovAdmin, HttpCurrentGovAdmin>();
    builder.Services.Configure<CookieSettings>(builder.Configuration.GetSection("Cookies"));
    builder.Services.Configure<CitizenGatewayAuthOptions>(builder.Configuration.GetSection(CitizenGatewayAuthOptions.SectionName));
    builder.Services.Configure<DeptGatewayAuthOptions>(builder.Configuration.GetSection(DeptGatewayAuthOptions.SectionName));
    builder.Services.AddProblemDetails();
    builder.Services.AddOpenApi();
    builder.Services.ConfigureHttpJsonOptions(o =>
    {
        o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
    builder.Services.AddControllers().AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    builder.Services.AddAuthentication("Seva")
        .AddScheme<AuthenticationSchemeOptions, SevaAuthHandler>("Seva", _ => { })
        .AddScheme<AuthenticationSchemeOptions, OperatorAuthHandler>("SevaOperator", _ => { })
        .AddScheme<AuthenticationSchemeOptions, AdminAuthHandler>("SevaAdmin", _ => { })
        .AddScheme<AuthenticationSchemeOptions, GovAdminAuthHandler>("SevaGovAdmin", _ => { });
    builder.Services.AddAuthorization();
    builder.Services.AddSevaOneOpenTelemetry("sevaone");

    builder.Services.AddHttpClient<Wso2HealthCheck>()
        .ConfigurePrimaryHttpMessageHandler(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<Wso2CitizenIssuerOptions>>().Value;
            var handler = new HttpClientHandler();
            if (opts.AcceptInsecureCertificates)
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            return handler;
        });
    builder.Services.AddHttpClient<Wso2ApimHealthCheck>()
        .ConfigurePrimaryHttpMessageHandler(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<Wso2CitizenIssuerOptions>>().Value;
            var handler = new HttpClientHandler();
            if (opts.AcceptInsecureCertificates)
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            return handler;
        });
    builder.Services.AddHealthChecks()
        .AddCheck<PostgresHealthCheck>("postgres")
        .AddCheck<RedisHealthCheck>("redis")
        .AddCheck<KafkaHealthCheck>("kafka")
        .AddCheck<Wso2HealthCheck>("wso2is")
        .AddCheck<Wso2ApimHealthCheck>("wso2apim");

    // Brute-force protection on credential-guessing endpoints (mobile OTP verify, operator
    // password) and on endpoints that trigger an external side effect per call (sending an
    // SMS, creating a DigiLocker session) so those can't be spammed either. Partitioned by
    // client IP -- behind a real reverse proxy this needs ForwardedHeaders wired up first, or
    // every request looks like it comes from the proxy's own IP and the limit becomes global
    // instead of per-caller (exactly what happens on a shared test VM behind nginx: every
    // browser hitting it collapses into one bucket). RateLimiting:Enabled defaults to true --
    // must stay on for any real deployment (KSDC included) -- and is only meant to be set to
    // false in appsettings.Development.json or a test environment's own local config override,
    // never in this shared appsettings.json.
    var rateLimitingEnabled = builder.Configuration.GetValue("RateLimiting:Enabled", true);
    var authRateLimitingEnabled = rateLimitingEnabled && builder.Configuration.GetValue("RateLimiting:AuthEnabled", true);
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (ctx, ct) =>
        {
            await ctx.HttpContext.Response.WriteAsJsonAsync(
                new { title = "Too many attempts. Try again in a few minutes." }, cancellationToken: ct);
        };

        options.AddPolicy("auth-credential", httpContext => authRateLimitingEnabled
            ? RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(5) })
            : RateLimitPartition.GetNoLimiter("disabled"));

        options.AddPolicy("auth-start", httpContext => authRateLimitingEnabled
            ? RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10) })
            : RateLimitPartition.GetNoLimiter("disabled"));

        // Deliberately much looser than the auth policies above -- those guard credential/OTP
        // guessing, a fundamentally different threat model, from a small number of attempts.
        // This guards a public click counter: a citizen genuinely browsing/comparing many
        // services (or chatting with the AI assistant) can plausibly click Apply a dozen+
        // times a minute. 60/min/IP still caps scripted click-spam far below anything that
        // could meaningfully skew the "popular services" ranking, without ever bothering a
        // real person.
        // Department Guide practice and test calls: a department trying its integration, some of which make this server call the
        // department's own address, so they are capped per caller address.
        options.AddPolicy("dept-portal", httpContext => rateLimitingEnabled
            ? RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) })
            : RateLimitPartition.GetNoLimiter("disabled"));

        options.AddPolicy("apply-click-track", httpContext => rateLimitingEnabled
            ? RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) })
            : RateLimitPartition.GetNoLimiter("disabled"));
    });

    var app = builder.Build();

    // Schema is applied by Flyway (database/migrations) as a separate deploy step, not at
    // app startup — Dapper has no migration runner of its own to hook in here.

    // Registered before the exception handler below so it observes the FINAL status code
    // (401/404/409/400 as translated by that handler) rather than the framework's raw 500
    // default at the point an exception is still propagating -- request logs would otherwise
    // show every domain-exception response as a false 500, misleading production monitoring.
    app.UseSerilogRequestLogging();

    app.Use(async (ctx, next) =>
    {
        try
        {
            await next();
        }
        catch (ValidationException ex)
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            await ctx.Response.WriteAsJsonAsync(new
            {
                code = SevaErrorCodes.Request.ValidationFailed,
                title = "Validation failed",
                errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }
        catch (ForbiddenException ex)
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsJsonAsync(new { code = ex.Code, title = ex.Message });
        }
        catch (NotFoundException ex)
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            await ctx.Response.WriteAsJsonAsync(new { code = ex.Code, title = ex.Message });
        }
        catch (DepartmentNotIntegratedException ex)
        {
            ctx.Response.StatusCode = StatusCodes.Status409Conflict;
            await ctx.Response.WriteAsJsonAsync(new { code = ex.Code, title = ex.Message, department = ex.DepartmentCode });
        }
        catch (ConflictException ex)
        {
            ctx.Response.StatusCode = StatusCodes.Status409Conflict;
            await ctx.Response.WriteAsJsonAsync(new { code = ex.Code, title = ex.Message });
        }
        catch (DomainException ex)
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            await ctx.Response.WriteAsJsonAsync(new { code = ex.Code, title = ex.Message });
        }
    });
    app.Use(async (ctx, next) =>
    {
        // Scalar's bootstrap page ships an inline <script type="module"> to call initialize() —
        // the citizen-facing CSP below has no 'unsafe-inline'/nonce for script-src, so it silently
        // blocks that inline script (external scalar.js/scalar.aspnetcore.js still load fine,
        // the page just never renders). Dev-docs routes aren't citizen-facing, so they get a
        // relaxed policy instead of loosening the real one.
        var isApiDocs = ctx.Request.Path.StartsWithSegments("/scalar") || ctx.Request.Path.StartsWithSegments("/openapi");
        ctx.Response.Headers["Content-Security-Policy"] = isApiDocs
            ? "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; font-src 'self' data:; img-src 'self' data: https:; connect-src 'self'; frame-ancestors 'none'"
            : "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; font-src 'self'; img-src 'self' data:; connect-src 'self'; worker-src 'self'; manifest-src 'self'; frame-ancestors 'none'";
        ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Response.Headers["X-Frame-Options"] = "DENY";
        ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        await next();
    });

    app.UseRateLimiter();
    app.UseMiddleware<CitizenGatewayAuthMiddleware>();
    app.UseMiddleware<DeptGatewayAuthMiddleware>();
    app.UseMiddleware<Wso2CitizenAuthMiddleware>();
    app.UseMiddleware<OperatorSessionMiddleware>();
    app.UseMiddleware<AdminSessionMiddleware>();
    app.UseMiddleware<GovAdminSessionMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "SevaOne  failed to start");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program
{
}
