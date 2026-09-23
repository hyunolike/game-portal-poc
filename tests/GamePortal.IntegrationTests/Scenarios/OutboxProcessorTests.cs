using System.Text.Json;
using GamePortal.Application.Abstractions;
using GamePortal.Application.Outbox;
using GamePortal.Domain.Outbox;
using GamePortal.Infrastructure.Outbox;
using GamePortal.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GamePortal.IntegrationTests.Scenarios;

[Collection(PortalCollection.Name)]
public class OutboxProcessorTests(PortalTestFixture fixture)
{
    [Fact]
    public async Task 게임_서버_전달에_성공하면_Processed_실패하면_백오프_후_재시도한다()
    {
        var ok = await InsertMessageAsync(accountId: 1);
        var fail = await InsertMessageAsync(accountId: 2);

        var gameServer = Substitute.For<IGameServerClient>();
        gameServer.SendItemsToMailboxAsync(Arg.Any<Guid>(), 2, Arg.Any<IReadOnlyList<GrantItem>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("503 Service Unavailable"));

        await DrainAsync(gameServer);

        await using var db = fixture.CreateDbContext();
        var processed = await db.OutboxMessages.AsNoTracking().SingleAsync(m => m.MessageId == ok);
        var failed = await db.OutboxMessages.AsNoTracking().SingleAsync(m => m.MessageId == fail);

        Assert.Equal(OutboxStatus.Processed, processed.Status);
        Assert.Equal(OutboxStatus.Pending, failed.Status);
        Assert.Equal(1, failed.AttemptCount);
        Assert.True(failed.NextAttemptAt > DateTimeOffset.UtcNow);
        Assert.Contains("503", failed.LastError);

        // 멱등키로 Outbox MessageId 를 전달
        await gameServer.Received(1).SendItemsToMailboxAsync(ok, 1, Arg.Any<IReadOnlyList<GrantItem>>(), "test", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task 여러_Worker_가_동시에_처리해도_메시지는_한_번만_전달된다()
    {
        var ids = new List<Guid>();
        for (var i = 0; i < 60; i++)
        {
            ids.Add(await InsertMessageAsync(accountId: 100 + i));
        }

        var delivered = new System.Collections.Concurrent.ConcurrentBag<Guid>();
        var gameServer = Substitute.For<IGameServerClient>();
        gameServer.SendItemsToMailboxAsync(Arg.Any<Guid>(), Arg.Any<long>(), Arg.Any<IReadOnlyList<GrantItem>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                delivered.Add(ci.ArgAt<Guid>(0));
                return Task.Delay(5);
            });

        // Worker 4대가 동시에 폴링
        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => DrainAsync(gameServer, batchSize: 7)));

        var ours = delivered.Where(ids.Contains).ToList();
        Assert.Equal(ids.Count, ours.Count);
        Assert.Equal(ids.Count, ours.Distinct().Count());
    }

    private async Task DrainAsync(IGameServerClient gameServer, int batchSize = 50)
    {
        while (true)
        {
            await using var db = fixture.CreateDbContext();
            var processor = new OutboxProcessor(
                db,
                gameServer,
                TimeProvider.System,
                Options.Create(new OutboxOptions { BatchSize = batchSize }),
                NullLogger<OutboxProcessor>.Instance);

            if (await processor.ProcessBatchAsync(CancellationToken.None) == 0)
            {
                return;
            }
        }
    }

    private async Task<Guid> InsertMessageAsync(long accountId)
    {
        var id = Guid.NewGuid();
        var payload = new ItemGrantPayload(id, accountId, [new GrantItem(1001, 1)], "test");
        await using var db = fixture.CreateDbContext();
        db.OutboxMessages.Add(OutboxMessage.Create(id, OutboxMessageTypes.ItemGrant, JsonSerializer.Serialize(payload), DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
        return id;
    }
}
