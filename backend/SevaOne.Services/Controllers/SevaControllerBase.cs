using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace SevaOne.Services.Controllers;

/// <summary>
/// Without MediatR there's no pipeline behavior to auto-validate requests, so each action that
/// takes a validated body/query calls ValidateAsync itself. Throws FluentValidation's own
/// ValidationException on failure — the global exception-handling middleware in Program.cs
/// turns that into a 400 with the same {title, errors} shape the original MediatR pipeline produced.
/// </summary>
[ApiController]
public abstract class SevaControllerBase : ControllerBase
{
    protected async Task ValidateAsync<T>(T request, CancellationToken ct)
    {
        var validator = HttpContext.RequestServices.GetService(typeof(IValidator<T>)) as IValidator<T>;
        if (validator is not null)
        {
            await validator.ValidateAndThrowAsync(request, ct);
        }
    }
}
