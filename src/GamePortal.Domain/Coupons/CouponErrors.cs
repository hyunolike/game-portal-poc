using GamePortal.Domain.Common;

namespace GamePortal.Domain.Coupons;

/// <summary>쿠폰 에러 코드. 웹/런처가 코드별로 다국어 메시지를 매핑하므로 코드 값은 변경 금지.</summary>
public static class CouponErrors
{
    public static readonly DomainError NotFound = new("COUPON_NOT_FOUND", "존재하지 않는 쿠폰 코드입니다.", ErrorKind.NotFound);
    public static readonly DomainError CampaignNotFound = new("COUPON_CAMPAIGN_NOT_FOUND", "쿠폰 캠페인을 찾을 수 없습니다.", ErrorKind.NotFound);
    public static readonly DomainError NotStarted = new("COUPON_NOT_STARTED", "아직 사용 기간이 아닙니다.");
    public static readonly DomainError Expired = new("COUPON_EXPIRED", "사용 기간이 만료된 쿠폰입니다.");
    public static readonly DomainError Disabled = new("COUPON_DISABLED", "사용이 중지된 쿠폰입니다.");
    public static readonly DomainError AlreadyUsed = new("COUPON_ALREADY_USED", "이미 사용된 쿠폰 코드입니다.", ErrorKind.Conflict);
    public static readonly DomainError AlreadyRedeemedByAccount = new("COUPON_ALREADY_REDEEMED", "이미 보상을 받은 쿠폰입니다.", ErrorKind.Conflict);
    public static readonly DomainError SoldOut = new("COUPON_SOLD_OUT", "선착순 수량이 모두 소진되었습니다.", ErrorKind.Conflict);
    public static readonly DomainError InvalidPeriod = new("COUPON_INVALID_PERIOD", "종료 시각은 시작 시각보다 늦어야 합니다.");
    public static readonly DomainError RewardRequired = new("COUPON_REWARD_REQUIRED", "보상 아이템을 1개 이상 지정해야 합니다.");
}
