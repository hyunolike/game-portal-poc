using FluentValidation;
using GamePortal.Domain.Coupons;

namespace GamePortal.Application.Coupons;

public sealed class RedeemCouponRequestValidator : AbstractValidator<RedeemCouponRequest>
{
    public RedeemCouponRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Must(code => CouponCode.Normalize(code ?? string.Empty) is { Length: >= 4 and <= CouponCode.MaxLength } normalized
                          && normalized.All(char.IsAsciiLetterOrDigit))
            .WithMessage("쿠폰 코드 형식이 올바르지 않습니다.");
    }
}

public sealed class CreateCampaignRequestValidator : AbstractValidator<CreateCampaignRequest>
{
    public const int MaxUniqueCodeCount = 100_000;

    public CreateCampaignRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.EndsAt).GreaterThan(x => x.StartsAt);
        RuleFor(x => x.MaxRedemptions).GreaterThan(0).When(x => x.MaxRedemptions is not null);
        RuleFor(x => x.Rewards).NotEmpty();
        RuleForEach(x => x.Rewards).ChildRules(r =>
        {
            r.RuleFor(i => i.ItemId).GreaterThan(0);
            r.RuleFor(i => i.Quantity).InclusiveBetween(1, RewardItem.MaxQuantity);
        });

        When(x => x.Type == CouponType.Shared, () =>
        {
            RuleFor(x => x.SharedCode)
                .NotEmpty()
                .Must(code => CouponCode.Normalize(code ?? string.Empty) is { Length: >= 4 and <= CouponCode.MaxLength } normalized
                              && normalized.All(char.IsAsciiLetterOrDigit))
                .WithMessage("공용 코드는 영문/숫자 4~20자여야 합니다.");
            RuleFor(x => x.UniqueCodeCount).Null();
        });

        When(x => x.Type == CouponType.Unique, () =>
        {
            RuleFor(x => x.UniqueCodeCount).NotNull().InclusiveBetween(1, MaxUniqueCodeCount);
            RuleFor(x => x.SharedCode).Empty();
        });
    }
}
