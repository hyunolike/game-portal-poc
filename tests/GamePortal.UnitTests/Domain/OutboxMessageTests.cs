using GamePortal.Domain.Outbox;

namespace GamePortal.UnitTests.Domain;

public class OutboxMessageTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(5, 32)]
    [InlineData(20, 600)] // 최대 10분
    public void 재시도_간격은_지수적으로_증가하고_상한이_있다(int attempt, int expectedSeconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), OutboxMessage.GetBackoff(attempt));
    }

    [Fact]
    public void 실패하면_다음_시도_시각이_밀린다()
    {
        var message = OutboxMessage.Create(Guid.NewGuid(), OutboxMessageTypes.ItemGrant, "{}", Now);

        message.MarkFailed("503", Now);

        Assert.Equal(OutboxStatus.Pending, message.Status);
        Assert.Equal(1, message.AttemptCount);
        Assert.Equal(Now.AddSeconds(2), message.NextAttemptAt);
        Assert.Equal("503", message.LastError);
    }

    [Fact]
    public void 최대_재시도_횟수를_넘으면_DeadLetter_로_전환되고_수동_재시도할_수_있다()
    {
        var message = OutboxMessage.Create(Guid.NewGuid(), OutboxMessageTypes.ItemGrant, "{}", Now);

        for (var i = 0; i < OutboxMessage.MaxAttempts; i++)
        {
            message.MarkFailed("timeout", Now);
        }

        Assert.Equal(OutboxStatus.Failed, message.Status);

        message.Requeue(Now.AddHours(1));

        Assert.Equal(OutboxStatus.Pending, message.Status);
        Assert.Equal(0, message.AttemptCount);
        Assert.Equal(Now.AddHours(1), message.NextAttemptAt);
    }

    [Fact]
    public void 대기중인_메시지는_수동_재시도할_수_없다()
    {
        var message = OutboxMessage.Create(Guid.NewGuid(), OutboxMessageTypes.ItemGrant, "{}", Now);

        Assert.Throws<InvalidOperationException>(() => message.Requeue(Now));
    }

    [Fact]
    public void 긴_에러_메시지는_잘라서_저장한다()
    {
        var message = OutboxMessage.Create(Guid.NewGuid(), OutboxMessageTypes.ItemGrant, "{}", Now);

        message.MarkFailed(new string('x', 5000), Now);

        Assert.Equal(2000, message.LastError!.Length);
    }
}
