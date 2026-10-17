using SevaOne.Business.Portal.DTOs;
using FluentValidation;

namespace SevaOne.Business.Portal.Validators;

public sealed class CreateApplyTokenValidator : AbstractValidator<CreateApplyTokenRequest>
{
    public CreateApplyTokenValidator()
    {
        RuleFor(x => x.ServiceCode).NotEmpty();
    }
}
