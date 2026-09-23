using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using GamePortal.Web.Auth;
using Microsoft.AspNetCore.Authentication;

namespace GamePortal.Web.ApiClient;

/// <summary>
/// Web.Api typed HttpClient.
/// 로그인 유저의 액세스 토큰은 서버 측 인증 쿠키에만 있고(BFF), 여기서 Authorization 헤더로 전달한다.
/// 브라우저 JS 에는 토큰이 노출되지 않는다.
/// </summary>
public sealed class PortalApiClient(HttpClient http, IHttpContextAccessor accessor)
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public Task<PagedResult<NoticeSummary>> GetNoticesAsync(NoticeCategory? category, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = $"api/v1/notices?page={page}&pageSize={pageSize}";
        if (category is not null)
        {
            query += $"&category={category}";
        }

        return SendAsync<PagedResult<NoticeSummary>>(HttpMethod.Get, query, body: null, authorize: false, cancellationToken);
    }

    public async Task<NoticeDetail?> GetNoticeAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            return await SendAsync<NoticeDetail>(HttpMethod.Get, $"api/v1/notices/{id}", body: null, authorize: false, cancellationToken);
        }
        catch (PortalApiException ex) when (ex.StatusCode == StatusCodes.Status404NotFound)
        {
            return null;
        }
    }

    public Task<RedeemResult> RedeemCouponAsync(string code, CancellationToken cancellationToken)
        => SendAsync<RedeemResult>(HttpMethod.Post, "api/v1/coupons/redeem", new { code }, authorize: true, cancellationToken);

    public Task<PagedResult<MyRedemption>> GetMyRedemptionsAsync(int page, CancellationToken cancellationToken)
        => SendAsync<PagedResult<MyRedemption>>(HttpMethod.Get, $"api/v1/coupons/history?page={page}&pageSize=10", body: null, authorize: true, cancellationToken);

    /// <summary>로컬/테스트 전용: Web.Api 의 개발용 토큰 발급 엔드포인트 호출</summary>
    public Task<DevTokenResponse> IssueDevTokenAsync(long accountId, string nickname, CancellationToken cancellationToken)
        => SendAsync<DevTokenResponse>(HttpMethod.Post, "dev/token", new { accountId, nickname }, authorize: false, cancellationToken);

    private async Task<T> SendAsync<T>(HttpMethod method, string uri, object? body, bool authorize, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        if (authorize)
        {
            var httpContext = accessor.HttpContext ?? throw new InvalidOperationException("HttpContext 가 없습니다.");
            var token = await httpContext.GetTokenAsync(PortalAuth.AccessTokenName);
            if (string.IsNullOrEmpty(token))
            {
                throw new PortalApiException(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "로그인이 필요합니다.", null);
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // 유저 IP 를 API 로 전달 (API 의 IP 기반 Rate limit / 로그가 프론트 서버 IP 로 뭉치지 않도록)
        var remoteIp = accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        if (remoteIp is not null)
        {
            request.Headers.TryAddWithoutValidation("X-Forwarded-For", remoteIp);
        }

        using var response = await http.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
                   ?? throw new PortalApiException((int)response.StatusCode, "EMPTY_RESPONSE", "빈 응답", null);
        }

        throw await ToExceptionAsync(response, cancellationToken);
    }

    private static async Task<PortalApiException> ToExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string? code = null, title = null, traceId = null;
        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = doc.RootElement;
            code = root.TryGetProperty("code", out var c) ? c.GetString() : null;
            title = root.TryGetProperty("title", out var t) ? t.GetString() : null;
            traceId = root.TryGetProperty("traceId", out var tr) ? tr.GetString() : null;
        }
        catch (JsonException)
        {
            // 게이트웨이 HTML 에러 페이지 등 ProblemDetails 가 아닌 응답
        }

        return new PortalApiException((int)response.StatusCode, code ?? $"HTTP_{(int)response.StatusCode}", title, traceId);
    }
}

public sealed record DevTokenResponse(string AccessToken);
