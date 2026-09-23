using System.Diagnostics;
using System.Text.Json.Serialization;
using GamePortal.Application;
using GamePortal.Application.Abstractions;
using GamePortal.AspNetCore.Auth;
using GamePortal.AspNetCore.ErrorHandling;
using GamePortal.Infrastructure;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Compact;

namespace GamePortal.AspNetCore;

/// <summary>
/// Web.Api / Admin.Api 공통 구성. 두 API 의 횡단 관심사(로깅, 에러, 인증, 헬스체크)를 한 곳에서 관리해
/// "한쪽만 고치고 다른 쪽은 잊는" 문제를 방지한다.
/// </summary>
public static class PortalApiDefaults
{
    public const string TestingEnvironment = "Testing";

    public static WebApplicationBuilder AddPortalApiDefaults(this WebApplicationBuilder builder, string apiTitle)
    {
        // 구조화 로그(JSON) → 운영에서는 stdout 을 수집기(Fluent Bit, CloudWatch 등)가 가져간다.
        builder.Host.UseSerilog((context, services, logger) =>
        {
            logger.ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", apiTitle);

            if (context.HostingEnvironment.IsDevelopment())
            {
                logger.WriteTo.Console();
            }
            else
            {
                logger.WriteTo.Console(new RenderedCompactJsonFormatter());
            }
        });

        var services = builder.Services;
        services.AddApplication();
        services.AddInfrastructure(builder.Configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddPortalJwtAuthentication(builder.Configuration);

        services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
        {
            ctx.ProblemDetails.Instance = ctx.HttpContext.Request.Path;
            ctx.ProblemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? ctx.HttpContext.TraceIdentifier;
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        var healthChecks = services.AddHealthChecks()
            .AddSqlServer(builder.Configuration.GetConnectionString("PortalDb")!, name: "sqlserver", tags: ["ready"]);
        var redis = builder.Configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redis))
        {
            healthChecks.AddRedis(redis, name: "redis", failureStatus: HealthStatus.Degraded, tags: ["ready"]);
        }

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new OpenApiInfo { Title = apiTitle, Version = "v1" });
            o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
            });
            o.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = [],
            });
        });

        // L4/L7 로드밸런서 뒤에서 실제 클라이언트 IP 를 얻기 위함 (감사 로그, Rate limit 파티션)
        services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            o.KnownNetworks.Clear();
            o.KnownProxies.Clear();
        });

        return builder;
    }

    public static WebApplication UsePortalApiDefaults(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment(TestingEnvironment))
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        // liveness: 프로세스 생존만 확인 (DB 장애로 파드가 재시작 루프에 빠지지 않도록)
        // 운영툴은 FallbackPolicy(인증 필수)가 걸려 있으므로 프로브용 엔드포인트는 명시적으로 익명 허용
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        // readiness: 의존성 확인 → 실패 시 LB 에서 제외
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        }).AllowAnonymous();

        app.MapControllers();
        return app;
    }

    public static bool IsDevelopmentOrTesting(this IHostEnvironment env) =>
        env.IsDevelopment() || env.IsEnvironment(TestingEnvironment);
}
