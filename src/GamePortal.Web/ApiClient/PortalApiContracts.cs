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

public sealed record RedeemResult(long RedemptionId, string CampaignName, IReadOnlyList<RewardItem> Rewards, DateTimeOffset RedeemedAt);

public sealed record MyRedemption(long RedemptionId, string CampaignName, DateTimeOffset RedeemedAt);
