using System.Text.Json;
using Dapper;
using GamePortal.Application.Abstractions;
using GamePortal.Application.Outbox;
using GamePortal.Domain.Outbox;
using GamePortal.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GamePortal.Infrastructure.Outbox;

/// <summary>
/// Outbox 배치 1회 처리. Worker 를 여러 대로 늘려도 같은 메시지를 동시에 처리하지 않도록
/// UPDLOCK + READPAST 로 "잠기지 않은 행만" 가져가 임대(lease)를 건다.
/// </summary>
public sealed class OutboxProcessor(
    PortalDbContext db,
    IGameServerClient gameServer,
    TimeProvider clock,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor> logger)
{
    // EF 로는 표현이 어려운 락 힌트 + OUTPUT 절이 필요한 쿼리라 Dapper 로 작성
    private const string ClaimSql = """
        WITH batch AS (
            SELECT TOP (@BatchSize) Id, LockedUntil
            FROM dbo.OutboxMessages WITH (ROWLOCK, UPDLOCK, READPAST)
            WHERE Status = 0
              AND NextAttemptAt <= @Now
              AND (LockedUntil IS NULL OR LockedUntil < @Now)
            ORDER BY NextAttemptAt, Id
        )
        UPDATE batch SET LockedUntil = @LeaseUntil
        OUTPUT inserted.Id;
        """;

    /// <returns>처리한 메시지 수</returns>
    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var now = clock.GetUtcNow();

        var connection = db.Database.GetDbConnection();
        var ids = (await connection.QueryAsync<long>(new CommandDefinition(
            ClaimSql,
            new { settings.BatchSize, Now = now, LeaseUntil = now + settings.LeaseDuration },
            cancellationToken: cancellationToken))).ToList();

        if (ids.Count == 0)
        {
            return 0;
        }

        var messages = await db.OutboxMessages
            .Where(m => ids.Contains(m.Id))
            .OrderBy(m => m.Id)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            await ProcessAsync(message, cancellationToken);

            // 메시지 단위로 커밋: 한 건 실패가 배치 전체 롤백으로 번지지 않도록
            await db.SaveChangesAsync(cancellationToken);
        }

        return messages.Count;
    }

    private async Task ProcessAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["OutboxMessageId"] = message.MessageId });
        try
        {
            switch (message.Type)
            {
                case OutboxMessageTypes.ItemGrant:
                    var payload = JsonSerializer.Deserialize<ItemGrantPayload>(message.Payload)
                                  ?? throw new InvalidOperationException("Empty payload");
                    await gameServer.SendItemsToMailboxAsync(payload.RequestId, payload.AccountId, payload.Items, payload.Reason, cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown outbox message type: {message.Type}");
            }

            message.MarkProcessed(clock.GetUtcNow());
            logger.LogInformation("Outbox message processed. Type={Type} Attempt={Attempt}", message.Type, message.AttemptCount + 1);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            message.MarkFailed(ex.Message, clock.GetUtcNow());

            if (message.Status == OutboxStatus.Failed)
            {
                // Dead letter → 알람 대상. 운영툴(Outbox 관리)에서 수동 재처리.
                logger.LogError(ex, "Outbox message moved to dead letter. Type={Type} Attempts={Attempts}", message.Type, message.AttemptCount);
            }
            else
            {
                logger.LogWarning(ex, "Outbox message failed. Type={Type} Attempts={Attempts} NextAttemptAt={NextAttemptAt}",
                    message.Type, message.AttemptCount, message.NextAttemptAt);
            }
        }
    }
}
