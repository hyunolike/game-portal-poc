using System.Text.Encodings.Web;
using System.Text.Unicode;
using GamePortal.Web.ApiClient;
using GamePortal.Web.Auth;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders;
using Polly;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "GamePortal Web")
    .WriteTo.Console());

// 기본 HtmlEncoder 는 한글을 &#xAC00; 형태로 인코딩한다 → 페이지 용량 증가, 검색엔진/소스 가독성 저하.
// 한글 범위는 그대로 출력하도록 허용 (<, >, &, " 등 HTML 특수문자는 여전히 인코딩됨)
builder.Services.Configure<WebEncoderOptions>(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizePage("/Coupon/Index");
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddPortalCookieAuth(builder.Environment);

builder.Services.AddOptions<PortalApiOptions>()
    .Bind(builder.Configuration.GetSection(PortalApiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<PortalApiClient>((sp, http) =>
    {
        var options = sp.GetRequiredService<IOptions<PortalApiOptions>>().Value;
        http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    })
    .AddStandardResilienceHandler(o =>
    {
        // 쿠폰 사용(POST)은 재시도하면 안 된다 — 첫 요청이 서버에서 처리됐는데 응답만 유실된 경우 중복 요청이 된다.
        // (서버가 UNIQUE 로 막긴 하지만 유저에게 "이미 사용" 에러를 보여주게 됨)
        o.Retry.ShouldHandle = args => ValueTask.FromResult(
            args.Context.GetRequestMessage()?.Method == HttpMethod.Get
            && HttpClientResiliencePredicates.IsTransient(args.Outcome));
        o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
    });

builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    // 기본 보안 헤더 (실서비스에서는 CDN/WAF 와 역할 분담)
    var headers = context.Response.Headers;
    headers.XContentTypeOptions = "nosniff";
    headers.XFrameOptions = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers.ContentSecurityPolicy =
        "default-src 'self'; style-src 'self' https://fonts.googleapis.com; font-src https://fonts.gstatic.com; img-src 'self' data:; frame-ancestors 'none'";
    await next();
});

app.UseStatusCodePagesWithReExecute("/status/{0}");
app.UseStaticFiles();
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapHealthChecks("/health/live");

app.Run();

/// <summary>WebApplicationFactory 에서 참조하기 위한 선언.</summary>
public partial class Program;
