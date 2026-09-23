namespace GamePortal.Domain.Outbox;

/// <summary>
/// Transactional Outbox.
/// 웹 DB 트랜잭션과 게임 서버 API 호출은 하나의 트랜잭션으로 묶을 수 없으므로,
/// "지급 요청"을 같은 트랜잭션에 기록하고 Worker 가 비동기로 전달한다. (at-least-once)
/// 게임 서버는 <see cref="MessageId"/> 로 멱등 처리해야 한다.
/// </summary>
public class OutboxMessage
{
    public const int MaxAttempts = 10;

    private OutboxMessage()
    {
        Type = string.Empty;
        Payload = string.Empty;
    }

    public long Id { get; private set; }

    public Guid MessageId { get; private set; }

    public string Type { get; private set; }

    public string Payload { get; private set; }

    public OutboxStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset NextAttemptAt { get; private set; }

    /// <summary>다중 Worker 인스턴스 간 중복 처리를 막기 위한 임대(lease) 만료 시각.</summary>
    public DateTimeOffset? LockedUntil { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public string? LastError { get; private set; }

    public static OutboxMessage Create(Guid messageId, string type, string payload, DateTimeOffset now) => new()
    {
        MessageId = messageId,
        Type = type,
        Payload = payload,
        Status = OutboxStatus.Pending,
        CreatedAt = now,
        NextAttemptAt = now,
    };

    public void MarkProcessed(DateTimeOffset now)
    {
        Status = OutboxStatus.Processed;
        ProcessedAt = now;
        LockedUntil = null;
        LastError = null;
    }

    public void MarkFailed(string error, DateTimeOffset now)
    {
        AttemptCount++;
        LastError = error.Length > 2000 ? error[..2000] : error;
        LockedUntil = null;

        if (AttemptCount >= MaxAttempts)
        {
            // Dead letter: 운영툴에서 원인 확인 후 수동 재시도
            Status = OutboxStatus.Failed;
            return;
        }

        NextAttemptAt = now + GetBackoff(AttemptCount);
    }

    /// <summary>운영자 수동 재시도.</summary>
    public void Requeue(DateTimeOffset now)
    {
        if (Status != OutboxStatus.Failed)
        {
            throw new InvalidOperationException("Failed 상태의 메시지만 재시도할 수 있습니다.");
        }

        Status = OutboxStatus.Pending;
        AttemptCount = 0;
        NextAttemptAt = now;
    }

    /// <summary>지수 백오프: 2^n 초, 최대 10분.</summary>
    public static TimeSpan GetBackoff(int attempt)
    {
        var seconds = Math.Min(Math.Pow(2, attempt), 600);
        return TimeSpan.FromSeconds(seconds);
    }
}

public enum OutboxStatus
{
    Pending = 0,
    Processed = 1,
    Failed = 2,
}

public static class OutboxMessageTypes
{
    public const string ItemGrant = "item.grant";
}
