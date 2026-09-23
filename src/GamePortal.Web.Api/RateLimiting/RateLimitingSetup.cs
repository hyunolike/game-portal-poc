using System.Threading.RateLimiting;
using GamePortal.AspNetCore.Auth;

namespace GamePortal.Web.Api.RateLimiting;

public static class RateLimitingSetup
{
    public const string CouponRedeemPolicy = "coupon-redeem";

    public static IServiceCollection AddPortalRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("RateLimiting").Get<RateLimitingOptions>() ?? new RateLimitingOptions();

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.OnRejected = async (ctx, ct) =>
            {
                if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    ctx.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                await ctx.HttpContext.Response.WriteAsJsonAsync(
                    new { status = 429, code = "TOO_MANY_REQUESTS", title = "요청이 너무 많습니다. 잠시 후 다시 시도해 주세요." },
                    ct);
            };

            // 전역: IP 당 (크롤러/매크로 1차 방어. 실제 운영은 WAF/CDN 에서 먼저 막는다)
            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                RateLimitPartition.GetTokenBucketLimiter(
                    ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = options.PerIpBurst,
                        TokensPerPeriod = options.PerIpPerSecond,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                        QueueLimit = 0,
                    }));

            // 쿠폰 입력: 계정당 분당 N회 (코드 무작위 대입 방지)
            o.AddPolicy(CouponRedeemPolicy, ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ctx.User.FindFirst(PortalClaimTypes.Subject)?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.CouponRedeemPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        return services;
    }
}

public sealed class RateLimitingOptions
{
    public int PerIpBurst { get; set; } = 100;

    public int PerIpPerSecond { get; set; } = 20;

    public int CouponRedeemPerMinute { get; set; } = 10;
}
