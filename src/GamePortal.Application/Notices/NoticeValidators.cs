using FluentValidation;
using GamePortal.Domain.Notices;

namespace GamePortal.Application.Notices;

public sealed class CreateNoticeRequestValidator : AbstractValidator<CreateNoticeRequest>
{
    public CreateNoticeRequestValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Notice.TitleMaxLength);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(100_000);
    }
}

public sealed class UpdateNoticeRequestValidator : AbstractValidator<UpdateNoticeRequest>
{
    public UpdateNoticeRequestValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Notice.TitleMaxLength);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(100_000);
    }
}
