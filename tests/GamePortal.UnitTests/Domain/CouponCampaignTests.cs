using GamePortal.Domain.Common;
using GamePortal.Domain.Coupons;

namespace GamePortal.UnitTests.Domain;

public class CouponCampaignTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = Start.AddDays(7);

    private static CouponCampaign NewCampaign(int? max = null) => CouponCampaign.Create(
        "10월 출석 이벤트", CouponType.Shared, Start, End, max, [new RewardItem(1001, 10)], operatorId: 1, now: Start.AddDays(-1));

    [Fact]
    public void 기간_내_활성_캠페인은_사용_가능하다()
    {
        var campaign = NewCampaign();

        var ex = Record.Exception(() => campaign.EnsureRedeemable(Start.AddHours(1)));

        Assert.Null(ex);
    }

    [Theory]
    [InlineData(-1, "COUPON_NOT_STARTED")]
    [InlineData(0, null)]
    [InlineData(7 * 24 * 60 - 1, null)]
    [InlineData(7 * 24 * 60, "COUPON_EXPIRED")] // 종료 시각은 미포함 [Start, End)
    public void 사용_기간_경계값(int minutesFromStart, string? expectedCode)
    {
        var campaign = NewCampaign();

        var ex = Record.Exception(() => campaign.EnsureRedeemable(Start.AddMinutes(minutesFromStart)));

        Assert.Equal(expectedCode, (ex as DomainException)?.Code);
    }

    [Fact]
    public void 비활성화된_캠페인은_사용할_수_없다()
    {
        var campaign = NewCampaign();
        campaign.Disable();

        var ex = Assert.Throws<DomainException>(() => campaign.EnsureRedeemable(Start.AddHours(1)));

        Assert.Equal(CouponErrors.Disabled.Code, ex.Code);
    }

    [Fact]
    public void 종료시각이_시작시각보다_빠르면_생성할_수_없다()
    {
        var ex = Assert.Throws<DomainException>(() => CouponCampaign.Create(
            "잘못된 기간", CouponType.Shared, End, Start, null, [new RewardItem(1, 1)], 1, Start));

        Assert.Equal(CouponErrors.InvalidPeriod.Code, ex.Code);
    }

    [Fact]
    public void 보상이_없으면_생성할_수_없다()
    {
        var ex = Assert.Throws<DomainException>(() => CouponCampaign.Create(
            "보상 없음", CouponType.Shared, Start, End, null, [], 1, Start));

        Assert.Equal(CouponErrors.RewardRequired.Code, ex.Code);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, RewardItem.MaxQuantity + 1)]
    public void 잘못된_보상_아이템은_거부된다(int itemId, int quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RewardItem(itemId, quantity));
    }
}
