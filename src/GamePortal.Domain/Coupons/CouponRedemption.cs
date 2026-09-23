namespace GamePortal.Domain.Coupons;

/// <summary>
/// 쿠폰 사용 이력. UNIQUE(CampaignId, AccountId) 로 "계정당 1회"를 DB 레벨에서 최종 보장한다.
/// CS 문의("쿠폰 보상 못 받았어요") 대응 시 조회 기준 테이블.
/// </summary>
public class CouponRedemption
{
    private CouponRedemption()
    {
    }

    public long Id { get; private set; }

    public long CampaignId { get; private set; }

    public long CouponCodeId { get; private set; }

    public long AccountId { get; private set; }

    public DateTimeOffset RedeemedAt { get; private set; }

    /// <summary>게임 서버 지급 요청(Outbox) 추적용 ID. 게임 서버에는 멱등키로 전달된다.</summary>
    public Guid GrantRequestId { get; private set; }

    public static CouponRedemption Create(long campaignId, long couponCodeId, long accountId, Guid grantRequestId, DateTimeOffset now) => new()
    {
        CampaignId = campaignId,
        CouponCodeId = couponCodeId,
        AccountId = accountId,
        GrantRequestId = grantRequestId,
        RedeemedAt = now,
    };
}
