using SevaOne.Business.Portal.DTOs;
using FluentValidation;

namespace SevaOne.Business.Portal.Validators;

public sealed class CreateApplyTokenValidator : AbstractValidator<CreateApplyTokenRequest>
{
    public CreateApplyTokenValidator()
    {
        RuleFor(x => x.ServiceCode).NotEmpty();
        RuleFor(x => x.Answers).Must(a => a is null || (a.Count <= 5 && a.All(p => p.Key is "Email" or "Key1" or "Key2" or "Key3" or "Key4" or "Key5" && p.Value is { Length: <= 200 })))
            .WithMessage("The answers are not valid.");
    }
}
