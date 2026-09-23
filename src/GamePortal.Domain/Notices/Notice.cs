using GamePortal.Domain.Common;

namespace GamePortal.Domain.Notices;

/// <summary>게임 홈페이지 공지사항. 운영툴에서 작성하고 웹사이트에 노출된다.</summary>
public class Notice : IAuditableEntity
{
    public const int TitleMaxLength = 200;

    private Notice()
    {
        Title = string.Empty;
        Content = string.Empty;
    }

    public long Id { get; private set; }

    public NoticeCategory Category { get; private set; }

    public string Title { get; private set; }

    public string Content { get; private set; }

    public bool IsPinned { get; private set; }

    public bool IsPublished { get; private set; }

    /// <summary>예약 게시 시각(UTC). 이 시각 이전에는 웹에 노출되지 않는다.</summary>
    public DateTimeOffset PublishAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public long CreatedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public long? UpdatedBy { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Notice Create(
        NoticeCategory category,
        string title,
        string content,
        bool isPinned,
        DateTimeOffset publishAt,
        long operatorId,
        DateTimeOffset now)
    {
        return new Notice
        {
            Category = category,
            Title = Guard.NotBlank(title, nameof(title), TitleMaxLength),
            Content = Guard.NotBlank(content, nameof(content), int.MaxValue),
            IsPinned = isPinned,
            IsPublished = true,
            PublishAt = publishAt,
            CreatedBy = operatorId,
            CreatedAt = now,
        };
    }

    public void Update(
        NoticeCategory category,
        string title,
        string content,
        bool isPinned,
        bool isPublished,
        DateTimeOffset publishAt,
        long operatorId,
        DateTimeOffset now)
    {
        EnsureNotDeleted();
        Category = category;
        Title = Guard.NotBlank(title, nameof(title), TitleMaxLength);
        Content = Guard.NotBlank(content, nameof(content), int.MaxValue);
        IsPinned = isPinned;
        IsPublished = isPublished;
        PublishAt = publishAt;
        UpdatedBy = operatorId;
        UpdatedAt = now;
    }

    /// <summary>
    /// 공지는 CS 대응(“어제 공지에 뭐라고 써있었냐”)을 위해 물리 삭제하지 않는다.
    /// </summary>
    public void Delete(long operatorId, DateTimeOffset now)
    {
        EnsureNotDeleted();
        IsDeleted = true;
        UpdatedBy = operatorId;
        UpdatedAt = now;
    }

    public bool IsVisibleAt(DateTimeOffset now) => !IsDeleted && IsPublished && PublishAt <= now;

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new DomainException(NoticeErrors.AlreadyDeleted);
        }
    }
}

public static class NoticeErrors
{
    public static readonly DomainError NotFound = new("NOTICE_NOT_FOUND", "공지사항을 찾을 수 없습니다.", ErrorKind.NotFound);
    public static readonly DomainError AlreadyDeleted = new("NOTICE_ALREADY_DELETED", "이미 삭제된 공지사항입니다.", ErrorKind.Conflict);
}
