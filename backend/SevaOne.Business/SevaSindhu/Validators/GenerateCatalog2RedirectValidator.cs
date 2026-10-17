using FluentValidation;
using SevaOne.Business.SevaSindhu.DTOs;

namespace SevaOne.Business.SevaSindhu.Validators;

public sealed class GenerateCatalog2RedirectValidator : AbstractValidator<GenerateCatalog2RedirectRequest>
{
    public GenerateCatalog2RedirectValidator()
    {
        RuleFor(x => x.ServiceId).NotEmpty();
    }
}
