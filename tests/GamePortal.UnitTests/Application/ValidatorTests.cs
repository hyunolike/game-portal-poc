using GamePortal.Application.Coupons;
using GamePortal.Domain.Coupons;

namespace GamePortal.UnitTests.Application;

public class ValidatorTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("ABCD-EFGH-2345", true)]
    [InlineData("welcome2026", true)]
    [InlineData("abc", false)]                       // 너무 짧음
    [InlineData("ABCDEFGHIJKLMNOPQRSTU", false)]     // 21자
    [InlineData("WELCOME'; DROP TABLE--", false)]    // 특수문자
    [InlineData("", false)]
    public void 쿠폰_입력_형식을_검증한다(string code, bool expected)
    {
        var result = new RedeemCouponRequestValidator().Validate(new RedeemCouponRequest(code));

        Assert.Equal(expected, result.IsValid);
    }

    [Fact]
    public void 공용_쿠폰은_코드가_필수이고_고유코드_수량은_없어야_한다()
    {
        var validator = new CreateCampaignRequestValidator();

        var missingCode = validator.Validate(Request(CouponType.Shared, sharedCode: null, count: null));
        var withCount = validator.Validate(Request(CouponType.Shared, sharedCode: "WELCOME", count: 10));
        var ok = validator.Validate(Request(CouponType.Shared, sharedCode: "WELCOME", count: null));

        Assert.False(missingCode.IsValid);
        Assert.False(withCount.IsValid);
        Assert.True(ok.IsValid);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(CreateCampaignRequestValidator.MaxUniqueCodeCount, true)]
    [InlineData(CreateCampaignRequestValidator.MaxUniqueCodeCount + 1, false)]
    public void 고유_쿠폰_발행_수량을_제한한다(int? count, bool expected)
    {
        var result = new CreateCampaignRequestValidator().Validate(Request(CouponType.Unique, sharedCode: null, count: count));

        Assert.Equal(expected, result.IsValid);
    }

    private static CreateCampaignRequest Request(CouponType type, string? sharedCode, int? count) => new(
        "캠페인", type, Start, Start.AddDays(1), null, [new RewardItemDto(1001, 1)], sharedCode, count);
}
