using SevaOne.Business.Portal.DTOs;
using FluentValidation;

namespace SevaOne.Business.Portal.Validators;

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Mobile).NotEmpty().MinimumLength(10).MaximumLength(15);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.District).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Taluk).NotEmpty().MaximumLength(120);
        RuleFor(x => x.StateName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.VillageName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DateOfBirth).NotNull();
        // Must match portal.sevaone_user's ck_sevaone_user_gender check constraint exactly.
        RuleFor(x => x.Gender).Must(g => g is "Male" or "Female" or "Other" or "PreferNotToSay")
            .WithMessage("Gender must be one of Male, Female, Other, PreferNotToSay.");
        RuleFor(x => x.Occupation).Must(o => OccupationCategories.IsValid(o))
            .WithMessage("Occupation is not a recognised category.");
        RuleFor(x => x.AadhaarLast4).Matches(@"^\d{4}$").When(x => !string.IsNullOrWhiteSpace(x.AadhaarLast4));
    }
}
