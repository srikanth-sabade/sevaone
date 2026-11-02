using SevaOne.Business.Portal.DTOs;
using FluentValidation;

namespace SevaOne.Business.Portal.Validators;

public sealed class GetUserDetailsValidator : AbstractValidator<GetUserDetailsRequest>
{
    public GetUserDetailsValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.Token).NotEmpty();
    }
}

public sealed class PostPaymentStatusValidator : AbstractValidator<PostPaymentStatusRequest>
{
    public PostPaymentStatusValidator()
    {
        RuleFor(x => x.DepartmentId).NotEmpty();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.PaymentStatus).NotEmpty();
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).When(x => x.Amount.HasValue);
    }
}
