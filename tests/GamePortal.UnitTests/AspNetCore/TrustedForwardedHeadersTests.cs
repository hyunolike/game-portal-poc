using System.Net;
using GamePortal.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GamePortal.UnitTests.AspNetCore;

public class TrustedForwardedHeadersTests
{
    [Fact]
    public async Task 신뢰하지_않는_출발지가_보낸_X_Forwarded_For는_무시한다()
    {
        var context = await SendAsync(remoteIp: "203.0.113.9", forwardedFor: "198.51.100.1", Settings());

        Assert.Equal(IPAddress.Parse("203.0.113.9"), context.Connection.RemoteIpAddress);
    }

    [Fact]
    public async Task 설정한_대역의_프록시가_보낸_X_Forwarded_For는_유저_IP로_쓴다()
    {
        var settings = Settings(("ForwardedHeaders:KnownNetworks:0", "10.0.0.0/8"));

        var context = await SendAsync(remoteIp: "10.1.2.3", forwardedFor: "198.51.100.1", settings);

        Assert.Equal(IPAddress.Parse("198.51.100.1"), context.Connection.RemoteIpAddress);
    }

    [Fact]
    public async Task 설정한_프록시_주소가_보낸_X_Forwarded_For는_유저_IP로_쓴다()
    {
        var settings = Settings(("ForwardedHeaders:KnownProxies:0", "192.0.2.10"));

        var context = await SendAsync(remoteIp: "192.0.2.10", forwardedFor: "198.51.100.1", settings);

        Assert.Equal(IPAddress.Parse("198.51.100.1"), context.Connection.RemoteIpAddress);
    }

    [Fact]
    public async Task 유저가_앞에_끼워_넣은_IP는_쓰지_않고_프록시가_본_IP를_쓴다()
    {
        var settings = Settings(("ForwardedHeaders:KnownNetworks:0", "10.0.0.0/8"));

        // 유저가 "1.1.1.1" 을 보내면 프록시는 그 뒤에 자신이 본 실제 IP 를 덧붙인다.
        var context = await SendAsync(remoteIp: "10.1.2.3", forwardedFor: "1.1.1.1, 198.51.100.1", settings);

        Assert.Equal(IPAddress.Parse("198.51.100.1"), context.Connection.RemoteIpAddress);
    }

    [Fact]
    public void 잘못_적은_대역은_기동할_때_실패한다()
    {
        var settings = Settings(("ForwardedHeaders:KnownNetworks:0", "10.0.0.0/99"));

        Assert.Throws<InvalidOperationException>(() => TrustedForwardedHeaders.Configure(new ForwardedHeadersOptions(), settings));
    }

    private static IConfiguration Settings(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    private static async Task<HttpContext> SendAsync(string remoteIp, string forwardedFor, IConfiguration settings)
    {
        var options = new ForwardedHeadersOptions();
        TrustedForwardedHeaders.Configure(options, settings);
        var middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance, Options.Create(options));

        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        context.Request.Headers["X-Forwarded-For"] = forwardedFor;

        await middleware.Invoke(context);
        return context;
    }
}
