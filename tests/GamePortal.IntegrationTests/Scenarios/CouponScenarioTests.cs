using System.Net;
using System.Net.Http.Json;
using GamePortal.Application.Common;
using GamePortal.Application.Coupons;
using GamePortal.Domain.Coupons;
using GamePortal.Domain.Outbox;
using GamePortal.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GamePortal.IntegrationTests.Scenarios;

[Collection(PortalCollection.Name)]
public class CouponScenarioTests(PortalTestFixture fixture)
{
    private static long _accountSeed = Random.Shared.NextInt64(1_000_000, 9_000_000);

    [Fact]
    public async Task 공용_쿠폰_사용시_보상이_Outbox_에_기록되고_내역에_남는다()
    {
        var code = NewCode();
        await CreateSharedCampaignAsync(code, maxRedemptions: null);
        var accountId = NextAccountId();
        var player = await fixture.Web.CreatePlayerClientAsync(accountId);

        var response = await player.PostAsJsonAsync("/api/v1/coupons/redeem", new RedeemCouponRequest(code.ToLowerInvariant()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.ReadAsAsync<RedeemCouponResult>();
        Assert.Equal(1001, Assert.Single(result.Rewards).ItemId);

        await using var db = fixture.CreateDbContext();
        var redemption = await db.CouponRedemptions.SingleAsync(r => r.Id == result.RedemptionId);
        var outbox = await db.OutboxMessages.SingleAsync(m => m.MessageId == redemption.GrantRequestId);
        Assert.Equal(OutboxStatus.Pending, outbox.Status);
        Assert.Contains($"\"AccountId\":{accountId}", outbox.Payload);

        var history = await (await player.GetAsync("/api/v1/coupons/history")).ReadAsAsync<PagedResult<MyRedemptionDto>>();
        Assert.Single(history.Items);
    }

    [Fact]
    public async Task 선착순_수량을_초과해_지급되지_않는다()
    {
        const int max = 10;
        const int players = 40;
        var code = NewCode();
        var campaignId = await CreateSharedCampaignAsync(code, maxRedemptions: max);

        var clients = await Task.WhenAll(Enumerable.Range(0, players).Select(_ => fixture.Web.CreatePlayerClientAsync(NextAccountId())));

        // 동시에 요청
        using var gate = new SemaphoreSlim(0);
        var tasks = clients.Select(async c =>
        {
            await gate.WaitAsync();
            return await c.PostAsJsonAsync("/api/v1/coupons/redeem", new RedeemCouponRequest(code));
        }).ToList();
        gate.Release(players);
        var responses = await Task.WhenAll(tasks);

        var success = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        var soldOut = 0;
        foreach (var r in responses.Where(r => r.StatusCode != HttpStatusCode.OK))
        {
            Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
            Assert.Equal("COUPON_SOLD_OUT", await r.ReadErrorCodeAsync());
            soldOut++;
        }

        Assert.Equal(max, success);
        Assert.Equal(players - max, soldOut);

        await using var db = fixture.CreateDbContext();
        var campaign = await db.CouponCampaigns.AsNoTracking().SingleAsync(c => c.Id == campaignId);
        Assert.Equal(max, campaign.RedeemedCount);
        Assert.Equal(max, await db.CouponRedemptions.CountAsync(r => r.CampaignId == campaignId));
    }

    [Fact]
    public async Task 같은_계정의_동시_요청은_한_번만_성공한다()
    {
        var code = NewCode();
        var campaignId = await CreateSharedCampaignAsync(code, maxRedemptions: 100);
        var player = await fixture.Web.CreatePlayerClientAsync(NextAccountId());

        var responses = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => player.PostAsJsonAsync("/api/v1/coupons/redeem", new RedeemCouponRequest(code))));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        foreach (var r in responses.Where(r => r.StatusCode != HttpStatusCode.OK))
        {
            Assert.Equal("COUPON_ALREADY_REDEEMED", await r.ReadErrorCodeAsync());
        }

        // 실패한 요청의 수량 증가는 롤백되어야 한다
        await using var db = fixture.CreateDbContext();
        Assert.Equal(1, (await db.CouponCampaigns.AsNoTracking().SingleAsync(c => c.Id == campaignId)).RedeemedCount);
    }

    [Fact]
    public async Task 고유_코드는_한_번만_사용된다_그리고_CSV_로_추출된다()
    {
        var admin = await fixture.Admin.CreateOperatorClientAsync("Admin");
        var create = await admin.PostAsJsonAsync("/api/v1/coupon-campaigns", new CreateCampaignRequest(
            "제휴 패키지", CouponType.Unique, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(30),
            null, [new RewardItemDto(2002, 1)], null, UniqueCodeCount: 1000), TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var campaignId = (await create.ReadAsAsync<IdResponse>()).Id;

        var csv = await admin.GetStringAsync($"/api/v1/coupon-campaigns/{campaignId}/codes.csv");
        var lines = csv.TrimStart('﻿').Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(1001, lines.Length); // header + 1000
        var code = lines[1].Split(',')[0]; // "ABCD-EFGH-2345" 형식 그대로 입력

        var a = await fixture.Web.CreatePlayerClientAsync(NextAccountId());
        var b = await fixture.Web.CreatePlayerClientAsync(NextAccountId());
        var responses = await Task.WhenAll(
            a.PostAsJsonAsync("/api/v1/coupons/redeem", new RedeemCouponRequest(code)),
            b.PostAsJsonAsync("/api/v1/coupons/redeem", new RedeemCouponRequest(code)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        var failed = Assert.Single(responses, r => r.StatusCode != HttpStatusCode.OK);
        Assert.Equal("COUPON_ALREADY_USED", await failed.ReadErrorCodeAsync());

        var detail = await (await admin.GetAsync($"/api/v1/coupon-campaigns/{campaignId}")).ReadAsAsync<CampaignDetailDto>();
        Assert.Equal(1000, detail.CodeCount);
        Assert.Equal(1, detail.RedeemedCount);
    }

    [Fact]
    public async Task 운영자가_중지한_쿠폰은_사용할_수_없다()
    {
        var code = NewCode();
        var campaignId = await CreateSharedCampaignAsync(code, null);
        var admin = await fixture.Admin.CreateOperatorClientAsync("Admin");
        (await admin.PostAsync($"/api/v1/coupon-campaigns/{campaignId}/disable", null)).EnsureSuccessStatusCode();

        var player = await fixture.Web.CreatePlayerClientAsync(NextAccountId());
        var response = await player.PostAsJsonAsync("/api/v1/coupons/redeem", new RedeemCouponRequest(code));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("COUPON_DISABLED", await response.ReadErrorCodeAsync());
    }

    [Theory]
    [InlineData("NOPE-NOPE-NOPE", HttpStatusCode.NotFound, "COUPON_NOT_FOUND")]
    [InlineData("!!", HttpStatusCode.BadRequest, "VALIDATION_FAILED")]
    public async Task 잘못된_코드(string code, HttpStatusCode status, string errorCode)
    {
        var player = await fixture.Web.CreatePlayerClientAsync(NextAccountId());

        var response = await player.PostAsJsonAsync("/api/v1/coupons/redeem", new RedeemCouponRequest(code));

        Assert.Equal(status, response.StatusCode);
        Assert.Equal(errorCode, await response.ReadErrorCodeAsync());
    }

    [Fact]
    public async Task 중복된_공용_코드는_발행할_수_없다()
    {
        var code = NewCode();
        await CreateSharedCampaignAsync(code, null);
        var admin = await fixture.Admin.CreateOperatorClientAsync("Admin");

        var response = await admin.PostAsJsonAsync("/api/v1/coupon-campaigns", SharedCampaign(code, null), TestJson.Options);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("COUPON_CODE_DUPLICATED", await response.ReadErrorCodeAsync());
    }

    [Fact]
    public async Task 로그인하지_않으면_쿠폰을_사용할_수_없다()
    {
        var response = await fixture.Web.CreateClient().PostAsJsonAsync("/api/v1/coupons/redeem", new RedeemCouponRequest("ABCD1234"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<long> CreateSharedCampaignAsync(string code, int? maxRedemptions)
    {
        var admin = await fixture.Admin.CreateOperatorClientAsync("Admin");
        var response = await admin.PostAsJsonAsync("/api/v1/coupon-campaigns", SharedCampaign(code, maxRedemptions), TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadAsAsync<IdResponse>()).Id;
    }

    private static CreateCampaignRequest SharedCampaign(string code, int? max) => new(
        "공용 쿠폰 " + code, CouponType.Shared, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(7),
        max, [new RewardItemDto(1001, 100)], code, null);

    private static string NewCode() => "T" + CouponCodeGenerator.Generate(11);

    private static long NextAccountId() => Interlocked.Increment(ref _accountSeed);

    private sealed record IdResponse(long Id);
}
