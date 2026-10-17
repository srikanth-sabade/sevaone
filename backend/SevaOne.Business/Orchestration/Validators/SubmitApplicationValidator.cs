using SevaOne.Business.Orchestration.DTOs;
using FluentValidation;

namespace SevaOne.Business.Orchestration.Validators;

public sealed class SubmitApplicationValidator : AbstractValidator<SubmitApplicationRequest>
{
    public SubmitApplicationValidator()
    {
        RuleFor(x => x.DepartmentCode).NotEmpty();
        RuleFor(x => x.ServiceCode).NotEmpty();
        RuleFor(x => x.ApplicantName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
    }
}
