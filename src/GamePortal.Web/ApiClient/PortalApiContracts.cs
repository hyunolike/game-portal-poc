namespace GamePortal.Web.ApiClient;

// Web.Api 응답 계약. 프론트는 서버 도메인 어셈블리를 참조하지 않고 필요한 필드만 정의한다.
// (API 에 필드가 추가돼도 깨지지 않도록 역직렬화는 관대하게)

public enum NoticeCategory
{
    Notice = 1,
    Update = 2,
    Event = 3,
    Maintenance = 4,
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public sealed record NoticeSummary(long Id, NoticeCategory Category, string Title, bool IsPinned, DateTimeOffset PublishAt);

public sealed record NoticeDetail(long Id, NoticeCategory Category, string Title, string Content, bool IsPinned, DateTimeOffset PublishAt);

public sealed record RewardItem(int ItemId, int Quantity);

// ServerName, CharacterName 이 비어 있으면 계정 보관함으로 받은 것이다.
public sealed record RedeemResult(
    long RedemptionId,
    string CampaignName,
    IReadOnlyList<RewardItem> Rewards,
    DateTimeOffset RedeemedAt,
    string? ServerName = null,
    string? CharacterName = null);

public sealed record MyRedemption(long RedemptionId, string CampaignName, DateTimeOffset RedeemedAt, string? ServerName = null, string? CharacterName = null);

public sealed record GameCharacter(int ServerId, string ServerName, long CharacterId, string Name, int Level);
