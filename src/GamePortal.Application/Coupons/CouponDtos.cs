using GamePortal.Domain.Coupons;

namespace GamePortal.Application.Coupons;

public sealed record RewardItemDto(int ItemId, int Quantity);

// ---- Web (플레이어) ----
public sealed record RedeemCouponRequest(string Code);

public sealed record RedeemCouponResult(long RedemptionId, string CampaignName, IReadOnlyList<RewardItemDto> Rewards, DateTimeOffset RedeemedAt);

public sealed record MyRedemptionDto(long RedemptionId, string CampaignName, DateTimeOffset RedeemedAt);

// ---- Admin (운영툴) ----
public sealed record CreateCampaignRequest(
    string Name,
    CouponType Type,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? MaxRedemptions,
    IReadOnlyList<RewardItemDto> Rewards,
    string? SharedCode,
    int? UniqueCodeCount);

public sealed record CampaignSummaryDto(
    long Id,
    string Name,
    CouponType Type,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? MaxRedemptions,
    int RedeemedCount,
    bool IsEnabled);

public sealed record CampaignDetailDto(
    long Id,
    string Name,
    CouponType Type,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? MaxRedemptions,
    int RedeemedCount,
    bool IsEnabled,
    IReadOnlyList<RewardItemDto> Rewards,
    int CodeCount,
    long CreatedBy,
    DateTimeOffset CreatedAt);

public sealed record CouponCodeExportRow(string Code, long? RedeemedByAccountId, DateTimeOffset? RedeemedAt);

public sealed record RedemptionAdminDto(
    long RedemptionId,
    long CampaignId,
    string CampaignName,
    string Code,
    long AccountId,
    DateTimeOffset RedeemedAt,
    Guid GrantRequestId,
    string? GrantStatus);
