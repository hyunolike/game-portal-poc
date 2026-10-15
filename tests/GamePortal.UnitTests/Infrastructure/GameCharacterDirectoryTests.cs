using System.Net;
using System.Text;
using GamePortal.Domain.Common;
using GamePortal.Infrastructure.GameServer;
using Microsoft.Extensions.Logging.Abstractions;

namespace GamePortal.UnitTests.Infrastructure;

public class GameCharacterDirectoryTests
{
    [Fact]
    public async Task 게임_서버_응답을_캐릭터로_바꾼다()
    {
        var sut = Create(_ => Json(HttpStatusCode.OK, """[{"serverId":1,"serverName":"아스테라","characterId":11,"name":"별의순례자","level":62}]"""));

        var characters = await sut.GetCharactersAsync(1, default);

        var c = Assert.Single(characters);
        Assert.Equal((1, "아스테라", 11L, "별의순례자", 62), (c.ServerId, c.ServerName, c.CharacterId, c.Name, c.Level));
    }

    [Fact]
    public async Task 캐릭터가_없는_계정의_404는_빈_목록이다()
    {
        var sut = Create(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        Assert.Empty(await sut.GetCharactersAsync(1, default));
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task 게임_서버_오류_응답은_연결할_수_없음으로_바꾼다(HttpStatusCode status)
    {
        var sut = Create(_ => new HttpResponseMessage(status));

        var ex = await Assert.ThrowsAsync<DomainException>(() => sut.GetCharactersAsync(1, default));
        Assert.Equal(GameServerErrors.Unavailable.Code, ex.Code);
    }

    [Fact]
    public async Task 연결_실패와_시간_초과도_연결할_수_없음으로_바꾼다()
    {
        var refused = Create(_ => throw new HttpRequestException("Connection refused"));
        var timeout = Create(_ => throw new TaskCanceledException("timeout"));

        Assert.Equal(GameServerErrors.Unavailable.Code, (await Assert.ThrowsAsync<DomainException>(() => refused.GetCharactersAsync(1, default))).Code);
        Assert.Equal(GameServerErrors.Unavailable.Code, (await Assert.ThrowsAsync<DomainException>(() => timeout.GetCharactersAsync(1, default))).Code);
    }

    [Fact]
    public async Task 요청한_쪽이_취소하면_취소로_끝낸다()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var sut = Create(_ => throw new TaskCanceledException());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.GetCharactersAsync(1, cts.Token));
    }

    private static GameCharacterDirectory Create(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new HttpClient(new StubHandler(respond)) { BaseAddress = new Uri("http://game-server.test/") }, NullLogger<GameCharacterDirectory>.Instance);

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(respond(request));
        }
    }
}
