using FluentValidation;
using SevaOne.Business.Portal.DTOs;
using SevaOne.Business.Status.DTOs;

namespace SevaOne.Business.Portal.Validators;

/// <summary>
/// DepartmentId is deliberately not validated here -- by the time these requests are
/// constructed, DeptController.TryResolveDepartment has already resolved it from the
/// authenticated machine-auth credentials, never from unchecked client input.
/// </summary>
public sealed class GetUserDetailsRequestValidator : AbstractValidator<GetUserDetailsRequest>
{
    public GetUserDetailsRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}

public sealed class PostPaymentStatusRequestValidator : AbstractValidator<PostPaymentStatusRequest>
{
    public PostPaymentStatusRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.PaymentStatus).NotEmpty();
    }
}

public sealed class CompleteApplicationRequestValidator : AbstractValidator<CompleteApplicationRequest>
{
    public CompleteApplicationRequestValidator()
    {
        RuleFor(x => x.SevaOneUserId).GreaterThan(0);
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.Status).NotEmpty();
    }
}

public sealed class ApplyDepartmentStatusRequestValidator : AbstractValidator<ApplyDepartmentStatusRequest>
{
    public ApplyDepartmentStatusRequestValidator()
    {
        RuleFor(x => x.DepartmentReference).NotEmpty();
        RuleFor(x => x.Status).NotEmpty();
        RuleFor(x => x.Note).NotEmpty();
    }
}

public sealed class CreateTestApplyTokenRequestValidator : AbstractValidator<CreateTestApplyTokenRequest>
{
    public CreateTestApplyTokenRequestValidator()
    {
        RuleFor(x => x.ServiceCode).NotEmpty();
    }
}
