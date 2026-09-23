using GamePortal.Domain.Notices;

namespace GamePortal.Application.Notices;

public sealed record NoticeSummaryDto(long Id, NoticeCategory Category, string Title, bool IsPinned, DateTimeOffset PublishAt);

public sealed record NoticeDetailDto(long Id, NoticeCategory Category, string Title, string Content, bool IsPinned, DateTimeOffset PublishAt);

public sealed record AdminNoticeDto(
    long Id,
    NoticeCategory Category,
    string Title,
    string Content,
    bool IsPinned,
    bool IsPublished,
    DateTimeOffset PublishAt,
    long CreatedBy,
    DateTimeOffset CreatedAt,
    long? UpdatedBy,
    DateTimeOffset? UpdatedAt);

public sealed record CreateNoticeRequest(NoticeCategory Category, string Title, string Content, bool IsPinned, DateTimeOffset? PublishAt);

public sealed record UpdateNoticeRequest(NoticeCategory Category, string Title, string Content, bool IsPinned, bool IsPublished, DateTimeOffset PublishAt);
