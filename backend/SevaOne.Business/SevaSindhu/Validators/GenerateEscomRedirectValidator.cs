using FluentValidation;
using SevaOne.Business.SevaSindhu.DTOs;

namespace SevaOne.Business.SevaSindhu.Validators;

public sealed class GenerateEscomRedirectValidator : AbstractValidator<GenerateEscomRedirectRequest>
{
    public GenerateEscomRedirectValidator()
    {
        RuleFor(x => x.ServiceId).NotEmpty();
    }
}
