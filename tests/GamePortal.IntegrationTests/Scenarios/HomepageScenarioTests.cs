using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using GamePortal.Application.Coupons;
using GamePortal.Application.Notices;
using GamePortal.Domain.Coupons;
using GamePortal.Domain.Notices;
using GamePortal.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GamePortal.IntegrationTests.Scenarios;

/// <summary>홈페이지(Razor Pages) → Web.Api → SQL Server 전 구간 시나리오.</summary>
[Collection(PortalCollection.Name)]
public partial class HomepageScenarioTests(PortalTestFixture fixture)
{
    private static long _accountSeed = Random.Shared.NextInt64(20_000_000, 30_000_000);

    [Fact]
    public async Task 홈_화면에_고정된_점검_공지가_상단_배너로_노출된다()
    {
        var title = $"정기 점검 안내 {Guid.NewGuid():N}"[..20];
        await CreateNoticeAsync(new CreateNoticeRequest(NoticeCategory.Maintenance, title, "06:00 ~ 10:00", IsPinned: true, PublishAt: null));

        var html = await fixture.Front.CreateClient().GetStringAsync("/");

        Assert.Contains("class=\"alert-bar\"", html);
        Assert.Contains(title, html);
    }

    [Fact]
    public async Task 공지_본문의_HTML_은_실행되지_않고_줄바꿈은_유지된다()
    {
        var id = await CreateNoticeAsync(new CreateNoticeRequest(
            NoticeCategory.Notice, "XSS 테스트", "첫 줄\n<script>alert('x')</script>\n\n둘째 문단", false, null));

        var html = await fixture.Front.CreateClient().GetStringAsync($"/notices/{id}");

        Assert.DoesNotContain("<script>alert", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("<br />", html);
    }

    [Fact]
    public async Task 공지_목록은_분류_탭으로_필터링된다()
    {
        var eventTitle = "이벤트 " + Guid.NewGuid().ToString("N")[..8];
        var updateTitle = "업데이트 " + Guid.NewGuid().ToString("N")[..8];
        await CreateNoticeAsync(new CreateNoticeRequest(NoticeCategory.Event, eventTitle, "내용", true, null));
        await CreateNoticeAsync(new CreateNoticeRequest(NoticeCategory.Update, updateTitle, "내용", true, null));

        var html = await fixture.Front.CreateClient().GetStringAsync("/notices?category=Event");

        Assert.Contains(eventTitle, html);
        Assert.DoesNotContain(updateTitle, html);
    }

    [Fact]
    public async Task 없는_공지는_404_안내_페이지를_보여준다()
    {
        var response = await fixture.Front.CreateClient().GetAsync("/notices/987654321");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("페이지를 찾을 수 없습니다", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task 로그인하지_않으면_쿠폰_페이지에서_로그인으로_이동한다()
    {
        var client = fixture.Front.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/coupon");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/account/login", response.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task 로그인_후_쿠폰을_등록하면_보상과_사용_내역이_표시되고_재등록은_안내_문구가_나온다()
    {
        var code = "WEB" + CouponCodeGenerator.Generate(9);
        await CreateSharedCampaignAsync(code, "홈페이지 오픈 기념");
        var client = fixture.Front.CreateClient();
        await LoginAsync(client, Interlocked.Increment(ref _accountSeed));

        // 하이픈/소문자로 입력해도 등록된다
        var first = await PostFormAsync(client, "/coupon", new() { ["Code"] = code.ToLowerInvariant().Insert(4, "-") });

        Assert.Contains("쿠폰이 등록되었습니다", first);
        Assert.Contains("에테르 주화", first);       // 아이템 ID 1001 → 표시명
        Assert.Contains("× 100", first);
        Assert.Contains("홈페이지 오픈 기념", first); // 사용 내역

        var second = await PostFormAsync(client, "/coupon", new() { ["Code"] = code });

        Assert.Contains("이미 보상을 받은 쿠폰입니다", second);
    }

    [Fact]
    public async Task 존재하지_않는_쿠폰은_서버_메시지가_아닌_안내_문구로_표시된다()
    {
        var client = fixture.Front.CreateClient();
        await LoginAsync(client, Interlocked.Increment(ref _accountSeed));

        var html = await PostFormAsync(client, "/coupon", new() { ["Code"] = "NOPE-NOPE-NOPE" });

        Assert.Contains("존재하지 않는 쿠폰 번호입니다", html);
    }

    [Fact]
    public async Task 로그인_후_외부_주소로_리다이렉트하지_않는다()
    {
        var client = fixture.Front.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = await GetAntiforgeryTokenAsync(client, "/account/login");

        var response = await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["AccountId"] = "1",
            ["Nickname"] = "tester",
            ["ReturnUrl"] = "https://evil.example.com/phish",
            ["__RequestVerificationToken"] = token,
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task 보안_헤더가_설정된다()
    {
        var response = await fixture.Front.CreateClient().GetAsync("/");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    private static async Task LoginAsync(HttpClient client, long accountId)
    {
        var html = await PostFormAsync(client, "/account/login", new()
        {
            ["AccountId"] = accountId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["Nickname"] = "테스터",
        });
        Assert.Contains("로그아웃", html);
    }

    /// <summary>폼 페이지를 GET 해서 Antiforgery 토큰을 얻은 뒤 POST (실제 브라우저와 같은 흐름)</summary>
    private static async Task<string> PostFormAsync(HttpClient client, string url, Dictionary<string, string> fields)
    {
        fields["__RequestVerificationToken"] = await GetAntiforgeryTokenAsync(client, url);
        var response = await client.PostAsync(url, new FormUrlEncodedContent(fields));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        return AntiforgeryToken().Match(html).Groups[1].Value;
    }

    private async Task<long> CreateNoticeAsync(CreateNoticeRequest request)
    {
        var admin = await fixture.Admin.CreateOperatorClientAsync("Operator");
        var response = await admin.PostAsJsonAsync("/api/v1/notices", request, TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadAsAsync<IdResponse>()).Id;
    }

    private async Task CreateSharedCampaignAsync(string code, string name)
    {
        var admin = await fixture.Admin.CreateOperatorClientAsync("Admin");
        var response = await admin.PostAsJsonAsync("/api/v1/coupon-campaigns", new CreateCampaignRequest(
            name, CouponType.Shared, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(7),
            null, [new RewardItemDto(1001, 100)], code, null), TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryToken();

    private sealed record IdResponse(long Id);
}
