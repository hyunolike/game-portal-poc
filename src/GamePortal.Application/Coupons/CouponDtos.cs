using GamePortal.Domain.Coupons;

namespace GamePortal.Application.Coupons;

public sealed record RewardItemDto(int ItemId, int Quantity);

// ---- Web (플레이어) ----
// CharacterId 를 비우면 계정 보관함(모든 캐릭터 공용)으로 받는다.
public sealed record RedeemCouponRequest(string Code, long? CharacterId = null);

// ServerName, CharacterName 은 보상을 받은 캐릭터. 계정 보관함으로 받았으면 둘 다 null.
public sealed record RedeemCouponResult(
    long RedemptionId,
    string CampaignName,
    IReadOnlyList<RewardItemDto> Rewards,
    DateTimeOffset RedeemedAt,
    string? ServerName = null,
    string? CharacterName = null);

public sealed record MyRedemptionDto(
    long RedemptionId,
    string CampaignName,
    DateTimeOffset RedeemedAt,
    string? ServerName = null,
    string? CharacterName = null);

// ---- Admin (운영툴) ----
public sealed record CreateCampaignRequest(
    string Name,
    CouponType Type,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? MaxRedemptions,
    IReadOnlyList<RewardItemDto> Rewards,
    string? SharedCode,
    int? UniqueCodeCount,
    int? MinCharacterLevel = null); // 대상 조건. null 이면 제한 없음

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
    DateTimeOffset CreatedAt,
    int? MinCharacterLevel = null);

public sealed record CouponCodeExportRow(string Code, long? RedeemedByAccountId, DateTimeOffset? RedeemedAt);

public sealed record RedemptionAdminDto(
    long RedemptionId,
    long CampaignId,
    string CampaignName,
    string Code,
    long AccountId,
    DateTimeOffset RedeemedAt,
    Guid GrantRequestId,
    string? GrantStatus,
    CouponType CampaignType, // 운영툴이 코드 표기(고유 코드만 4자리 구분)를 결정하는 데 사용. 필드 추가만 하므로 하위 호환
    string? ServerName = null, // 보상을 받은 캐릭터. 둘 다 null 이면 계정 보관함으로 받은 것
    string? CharacterName = null);
