using System.Net;
using System.Net.Http.Json;
using GamePortal.Application.Auditing;
using GamePortal.Application.Common;
using GamePortal.Application.Notices;
using GamePortal.Domain.Notices;
using GamePortal.IntegrationTests.Infrastructure;

namespace GamePortal.IntegrationTests.Scenarios;

[Collection(PortalCollection.Name)]
public class NoticeScenarioTests(PortalTestFixture fixture)
{
    [Fact]
    public async Task 운영툴에서_작성한_공지가_웹에_노출되고_수정시_캐시가_무효화된다()
    {
        var admin = await fixture.Admin.CreateOperatorClientAsync("Operator");
        var web = fixture.Web.CreateClient();

        // 1. 웹 목록을 먼저 조회해 캐시를 채운다
        (await web.GetAsync("/api/v1/notices?category=Update")).EnsureSuccessStatusCode();

        // 2. 운영툴에서 공지 작성
        var create = await admin.PostAsJsonAsync("/api/v1/notices",
            new CreateNoticeRequest(NoticeCategory.Update, "9월 업데이트 안내", "신규 던전 추가", IsPinned: true, PublishAt: null), TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = (await create.ReadAsAsync<IdResponse>()).Id;

        // 3. 캐시가 무효화되어 즉시 노출
        var list = await (await web.GetAsync("/api/v1/notices?category=Update")).ReadAsAsync<PagedResult<NoticeSummaryDto>>();
        Assert.Contains(list.Items, n => n.Id == id && n.IsPinned);

        // 4. 수정 → 상세에도 반영
        var update = await admin.PutAsJsonAsync($"/api/v1/notices/{id}",
            new UpdateNoticeRequest(NoticeCategory.Update, "9월 업데이트 안내 (수정)", "신규 던전 추가", true, true, DateTimeOffset.UtcNow.AddMinutes(-1)), TestJson.Options);
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var detail = await (await web.GetAsync($"/api/v1/notices/{id}")).ReadAsAsync<NoticeDetailDto>();
        Assert.Equal("9월 업데이트 안내 (수정)", detail.Title);

        // 5. 삭제 → 404
        (await admin.DeleteAsync($"/api/v1/notices/{id}")).EnsureSuccessStatusCode();
        var deleted = await web.GetAsync($"/api/v1/notices/{id}");
        Assert.Equal(HttpStatusCode.NotFound, deleted.StatusCode);
        Assert.Equal("NOTICE_NOT_FOUND", await deleted.ReadErrorCodeAsync());
    }

    [Fact]
    public async Task 예약_공지는_게시_시각_전에는_웹에_노출되지_않는다()
    {
        var admin = await fixture.Admin.CreateOperatorClientAsync("Operator");
        var create = await admin.PostAsJsonAsync("/api/v1/notices",
            new CreateNoticeRequest(NoticeCategory.Maintenance, "내일 정기 점검", "06:00~10:00", false, DateTimeOffset.UtcNow.AddDays(1)), TestJson.Options);
        var id = (await create.ReadAsAsync<IdResponse>()).Id;

        var response = await fixture.Web.CreateClient().GetAsync($"/api/v1/notices/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task 공지_변경은_감사_로그에_기록된다()
    {
        var operatorClient = await fixture.Admin.CreateOperatorClientAsync("Operator");
        var create = await operatorClient.PostAsJsonAsync("/api/v1/notices",
            new CreateNoticeRequest(NoticeCategory.Event, "감사로그 테스트", "내용", false, null), TestJson.Options);
        var id = (await create.ReadAsAsync<IdResponse>()).Id;
        await operatorClient.PutAsJsonAsync($"/api/v1/notices/{id}",
            new UpdateNoticeRequest(NoticeCategory.Event, "감사로그 테스트 (수정)", "내용", false, true, DateTimeOffset.UtcNow), TestJson.Options);

        var admin = await fixture.Admin.CreateOperatorClientAsync("Admin");
        var logs = await (await admin.GetAsync($"/api/v1/audit-logs?entityName=Notice&entityId={id}")).ReadAsAsync<PagedResult<AuditLogDto>>();

        Assert.Equal(2, logs.TotalCount);
        var modified = Assert.Single(logs.Items, l => l.Action == "Modified");
        Assert.Equal(9001, modified.OperatorId);
        Assert.Contains("감사로그 테스트 (수정)", modified.Changes);
        Assert.DoesNotContain("\"Content\"", modified.Changes); // 바뀐 컬럼만 기록
    }

    [Fact]
    public async Task 잘못된_요청은_ValidationProblem_으로_응답한다()
    {
        var admin = await fixture.Admin.CreateOperatorClientAsync("Operator");

        var response = await admin.PostAsJsonAsync("/api/v1/notices",
            new CreateNoticeRequest(NoticeCategory.Notice, "", "", false, null), TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_FAILED", await response.ReadErrorCodeAsync());
    }

    private sealed record IdResponse(long Id);
}
