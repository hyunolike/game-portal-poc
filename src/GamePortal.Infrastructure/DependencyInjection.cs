using GamePortal.Application.Abstractions;
using GamePortal.Infrastructure.Caching;
using GamePortal.Infrastructure.GameServer;
using GamePortal.Infrastructure.Outbox;
using GamePortal.Infrastructure.Persistence;
using GamePortal.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GamePortal.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PortalDb")
                               ?? throw new InvalidOperationException("ConnectionStrings:PortalDb 가 설정되지 않았습니다.");

        services.AddDbContext<PortalDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                // 일시적 오류(페일오버, 네트워크 순단) 자동 재시도
                sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(2), errorNumbersToAdd: null);
                sql.CommandTimeout(30);
            });
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
        });
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IPortalDbContext>(sp => sp.GetRequiredService<PortalDbContext>());
        services.AddScoped<ICouponCodeBulkWriter, CouponCodeBulkWriter>();

        var redis = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redis))
        {
            // 로컬 개발/단일 인스턴스 용. 다중 인스턴스 운영 환경에서는 반드시 Redis 사용.
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(o =>
            {
                o.Configuration = redis;
                o.InstanceName = "gameportal:";
            });
        }

        services.AddSingleton<ICacheService, DistributedCacheService>();
        return services;
    }

    /// <summary>운영툴 전용: IAuditableEntity 변경 시 AuditLog 자동 기록.</summary>
    public static IServiceCollection AddAuditing(this IServiceCollection services)
    {
        services.AddScoped<ISaveChangesInterceptor, AuditSaveChangesInterceptor>();
        return services;
    }

    /// <summary>Worker 전용: Outbox → 게임 서버 전달.</summary>
    public static IServiceCollection AddOutboxDispatcher(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<GameServerOptions>()
            .Bind(configuration.GetSection(GameServerOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IGameServerClient, GameServerClient>((sp, http) =>
            {
                var o = sp.GetRequiredService<IOptions<GameServerOptions>>().Value;
                http.BaseAddress = new Uri(o.BaseUrl.TrimEnd('/') + "/");
                http.DefaultRequestHeaders.Add("X-Api-Key", o.ApiKey);
            })
            .AddStandardResilienceHandler(o =>
            {
                // Outbox 가 긴 주기의 재시도를 담당하므로 HTTP 레벨 재시도는 짧게
                o.Retry.MaxRetryAttempts = 2;
                o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
                o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);
            });

        services.AddScoped<OutboxProcessor>();
        services.AddHostedService<OutboxDispatcher>();
        return services;
    }
}
