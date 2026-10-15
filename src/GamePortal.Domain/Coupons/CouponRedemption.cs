using GamePortal.Domain.Characters;

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

    // 보상을 받은 캐릭터. 비어 있으면 계정 보관함(모든 캐릭터 공용)으로 지급한 것이다.
    // 서버와 캐릭터 이름은 사용 시점의 값을 남긴다. 나중에 캐릭터 이름이 바뀌어도 CS 가 당시 기록으로 답할 수 있게 한다.
    public int? ServerId { get; private set; }

    public string? ServerName { get; private set; }

    public long? CharacterId { get; private set; }

    public string? CharacterName { get; private set; }

    public static CouponRedemption Create(
        long campaignId,
        long couponCodeId,
        long accountId,
        Guid grantRequestId,
        DateTimeOffset now,
        GameCharacter? recipient = null) => new()
        {
            CampaignId = campaignId,
            CouponCodeId = couponCodeId,
            AccountId = accountId,
            GrantRequestId = grantRequestId,
            RedeemedAt = now,
            ServerId = recipient?.ServerId,
            ServerName = recipient?.ServerName,
            CharacterId = recipient?.CharacterId,
            CharacterName = recipient?.Name,
        };
}
