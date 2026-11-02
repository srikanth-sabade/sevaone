using SevaOne.Business.Discovery.DTOs;
using FluentValidation;

namespace SevaOne.Business.Discovery.Validators;

public sealed class SearchServicesValidator : AbstractValidator<SearchServicesRequest>
{
    public SearchServicesValidator()
    {
        RuleFor(x => x.Q).NotEmpty().MinimumLength(2).MaximumLength(200);
    }
}
