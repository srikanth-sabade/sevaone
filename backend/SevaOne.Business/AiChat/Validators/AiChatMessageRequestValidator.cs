using FluentValidation;
using SevaOne.Business.AiChat.DTOs;

namespace SevaOne.Business.AiChat.Validators;

public sealed class AiChatMessageRequestValidator : AbstractValidator<AiChatMessageRequest>
{
    public AiChatMessageRequestValidator()
    {
        RuleFor(x => x.QueryText).NotEmpty();
    }
}
