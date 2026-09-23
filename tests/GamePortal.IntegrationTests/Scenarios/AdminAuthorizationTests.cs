using System.Net;
using System.Net.Http.Json;
using GamePortal.Application.Notices;
using GamePortal.Domain.Notices;
using GamePortal.IntegrationTests.Infrastructure;

namespace GamePortal.IntegrationTests.Scenarios;

[Collection(PortalCollection.Name)]
public class AdminAuthorizationTests(PortalTestFixture fixture)
{
    [Theory]
    [InlineData("GET", "/api/v1/notices")]
    [InlineData("GET", "/api/v1/coupon-campaigns")]
    [InlineData("GET", "/api/v1/audit-logs")]
    public async Task 토큰이_없으면_401(string method, string url)
    {
        var response = await fixture.Admin.CreateClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task 헬스체크는_인증_없이_접근_가능하다(string url)
    {
        // K8s/LB 프로브는 토큰이 없다. FallbackPolicy 에 막히면 전 파드가 NotReady 가 된다.
        var admin = await fixture.Admin.CreateClient().GetAsync(url);
        var web = await fixture.Web.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, web.StatusCode);
    }

    [Fact]
    public async Task 플레이어용_토큰으로는_운영툴에_접근할_수_없다()
    {
        // Web 용 토큰은 audience/서명키가 달라 Admin 에서 거부되어야 한다
        var player = await fixture.Web.CreatePlayerClientAsync(accountId: 1);
        var admin = fixture.Admin.CreateClient();
        admin.DefaultRequestHeaders.Authorization = player.DefaultRequestHeaders.Authorization;

        var response = await admin.GetAsync("/api/v1/notices");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CS_는_조회만_가능하다()
    {
        var cs = await fixture.Admin.CreateOperatorClientAsync("CS");

        var read = await cs.GetAsync("/api/v1/coupon-redemptions?accountId=1");
        var write = await cs.PostAsJsonAsync("/api/v1/notices",
            new CreateNoticeRequest(NoticeCategory.Notice, "t", "c", false, null), TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
    }

    [Fact]
    public async Task 운영자는_쿠폰을_발행할_수_없다()
    {
        var op = await fixture.Admin.CreateOperatorClientAsync("Operator");

        var response = await op.PostAsJsonAsync("/api/v1/coupon-campaigns", new { name = "x" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
