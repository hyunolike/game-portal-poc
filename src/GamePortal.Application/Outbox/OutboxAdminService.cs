using GamePortal.Application.Abstractions;
using GamePortal.Application.Common;
using GamePortal.Domain.Common;
using GamePortal.Domain.Outbox;
using Microsoft.EntityFrameworkCore;

namespace GamePortal.Application.Outbox;

public sealed record OutboxMessageDto(
    long Id,
    Guid MessageId,
    string Type,
    OutboxStatus Status,
    int AttemptCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset NextAttemptAt,
    DateTimeOffset? ProcessedAt,
    string? LastError,
    string Payload);

public sealed record OutboxStatsDto(int Pending, int Failed, int ProcessedLastHour);

/// <summary>운영툴: 게임 서버 지급 실패 건(Dead letter) 모니터링 및 수동 재처리.</summary>
public sealed class OutboxAdminService(IPortalDbContext db, TimeProvider clock)
{
    public static readonly DomainError NotFound = new("OUTBOX_NOT_FOUND", "메시지를 찾을 수 없습니다.", ErrorKind.NotFound);
    public static readonly DomainError NotFailed = new("OUTBOX_NOT_FAILED", "실패 상태의 메시지만 재시도할 수 있습니다.", ErrorKind.Conflict);

    public async Task<OutboxStatsDto> GetStatsAsync(CancellationToken cancellationToken)
    {
        var since = clock.GetUtcNow().AddHours(-1);
        var pending = await db.OutboxMessages.CountAsync(m => m.Status == OutboxStatus.Pending, cancellationToken);
        var failed = await db.OutboxMessages.CountAsync(m => m.Status == OutboxStatus.Failed, cancellationToken);
        var processed = await db.OutboxMessages.CountAsync(m => m.Status == OutboxStatus.Processed && m.ProcessedAt >= since, cancellationToken);
        return new OutboxStatsDto(pending, failed, processed);
    }

    public Task<PagedResult<OutboxMessageDto>> GetListAsync(OutboxStatus status, PageRequest page, CancellationToken cancellationToken)
    {
        return db.OutboxMessages.AsNoTracking()
            .Where(m => m.Status == status)
            .OrderByDescending(m => m.Id)
            .Select(m => new OutboxMessageDto(
                m.Id, m.MessageId, m.Type, m.Status, m.AttemptCount, m.CreatedAt, m.NextAttemptAt, m.ProcessedAt, m.LastError, m.Payload))
            .ToPagedResultAsync(page, cancellationToken);
    }

    public async Task RetryAsync(long id, CancellationToken cancellationToken)
    {
        var message = await db.OutboxMessages.SingleOrDefaultAsync(m => m.Id == id, cancellationToken)
                      ?? throw new DomainException(NotFound);

        if (message.Status != OutboxStatus.Failed)
        {
            throw new DomainException(NotFailed);
        }

        message.Requeue(clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }
}
