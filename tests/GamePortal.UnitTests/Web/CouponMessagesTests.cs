using System.Reflection;
using GamePortal.Domain.Common;
using GamePortal.Domain.Coupons;
using GamePortal.Web;

namespace GamePortal.UnitTests.Web;

public class CouponMessagesTests
{
    // 운영툴 전용 에러(유저 쿠폰 등록 경로에서는 발생하지 않음)
    private static readonly HashSet<string> AdminOnlyCodes = ["COUPON_CAMPAIGN_NOT_FOUND", "COUPON_INVALID_PERIOD", "COUPON_REWARD_REQUIRED"];

    public static TheoryData<string> RedeemErrorCodes()
    {
        var data = new TheoryData<string>();
        foreach (var field in typeof(CouponErrors).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is DomainError error && !AdminOnlyCodes.Contains(error.Code))
            {
                data.Add(error.Code);
            }
        }

        return data;
    }

    /// <summary>
    /// 계약 테스트: 서버에 새 쿠폰 에러 코드를 추가하면 홈페이지 안내 문구도 함께 추가해야 한다.
    /// (누락 시 유저에게 "일시적인 오류" 로 잘못 안내됨)
    /// </summary>
    [Theory]
    [MemberData(nameof(RedeemErrorCodes))]
    public void 서버의_모든_쿠폰_에러_코드에_안내_문구가_있다(string code)
    {
        Assert.NotEqual(CouponMessages.For("__unknown__"), CouponMessages.For(code));
    }

    [Fact]
    public void 알_수_없는_코드는_일반_안내_문구로_대체한다()
    {
        Assert.Contains("잠시 후 다시 시도", CouponMessages.For("SOMETHING_NEW"));
    }

    [Fact]
    public void 카탈로그에_없는_아이템은_ID_로_표시한다()
    {
        Assert.Equal("에테르 주화", ItemCatalog.NameOf(1001));
        Assert.Equal("아이템 #9999", ItemCatalog.NameOf(9999));
    }
}
