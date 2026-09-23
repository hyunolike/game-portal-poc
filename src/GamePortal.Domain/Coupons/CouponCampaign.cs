using GamePortal.Domain.Common;

namespace GamePortal.Domain.Coupons;

/// <summary>
/// 쿠폰 캠페인(발행 단위). 기간/수량/보상을 정의하고, 하위에 <see cref="CouponCode"/> 를 가진다.
/// </summary>
public class CouponCampaign : IAuditableEntity
{
    private readonly List<RewardItem> _rewards = [];

    private CouponCampaign()
    {
        Name = string.Empty;
    }

    public long Id { get; private set; }

    public string Name { get; private set; }

    public CouponType Type { get; private set; }

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>총 사용 가능 수량(선착순). null 이면 무제한.</summary>
    public int? MaxRedemptions { get; private set; }

    /// <summary>
    /// 사용된 수량. 동시성 제어를 위해 애플리케이션에서 증가시키지 않고
    /// 조건부 UPDATE(원자적 증가)로만 변경한다. <c>CouponRedeemService</c> 참고.
    /// </summary>
    public int RedeemedCount { get; private set; }

    public bool IsEnabled { get; private set; }

    public IReadOnlyList<RewardItem> Rewards => _rewards;

    public long CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static CouponCampaign Create(
        string name,
        CouponType type,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        int? maxRedemptions,
        IEnumerable<RewardItem> rewards,
        long operatorId,
        DateTimeOffset now)
    {
        if (endsAt <= startsAt)
        {
            throw new DomainException(CouponErrors.InvalidPeriod);
        }

        if (maxRedemptions is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRedemptions));
        }

        var campaign = new CouponCampaign
        {
            Name = Guard.NotBlank(name, nameof(name), 100),
            Type = type,
            StartsAt = startsAt,
            EndsAt = endsAt,
            MaxRedemptions = maxRedemptions,
            IsEnabled = true,
            CreatedBy = operatorId,
            CreatedAt = now,
        };
        campaign._rewards.AddRange(rewards);

        if (campaign._rewards.Count == 0)
        {
            throw new DomainException(CouponErrors.RewardRequired);
        }

        return campaign;
    }

    /// <summary>
    /// 사용 가능 여부(기간/활성 상태) 검증. 수량 소진 여부는 동시성 때문에 여기서 판단하지 않는다.
    /// </summary>
    public void EnsureRedeemable(DateTimeOffset now)
    {
        if (!IsEnabled)
        {
            throw new DomainException(CouponErrors.Disabled);
        }

        if (now < StartsAt)
        {
            throw new DomainException(CouponErrors.NotStarted);
        }

        if (now >= EndsAt)
        {
            throw new DomainException(CouponErrors.Expired);
        }
    }

    /// <summary>운영 중 이슈(오지급, 코드 유출 등) 발생 시 즉시 사용 중지.</summary>
    public void Disable() => IsEnabled = false;

    public void Enable() => IsEnabled = true;
}
