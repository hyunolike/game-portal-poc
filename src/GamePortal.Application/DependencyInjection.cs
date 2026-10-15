using FluentValidation;
using GamePortal.Application.Auditing;
using GamePortal.Application.Characters;
using GamePortal.Application.Coupons;
using GamePortal.Application.Notices;
using GamePortal.Application.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GamePortal.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<NoticeQueryService>();
        services.AddScoped<NoticeAdminService>();
        services.AddScoped<CouponCampaignAdminService>();
        services.AddScoped<OutboxAdminService>();
        services.AddScoped<AuditLogQueryService>();

        return services;
    }

    /// <summary>
    /// 유저 API(Web.Api) 전용 유스케이스. 게임 서버 캐릭터 조회(<see cref="Abstractions.IGameCharacterDirectory"/>)가 필요해
    /// 운영툴 API 에는 등록하지 않는다.
    /// </summary>
    public static IServiceCollection AddPlayerUseCases(this IServiceCollection services)
    {
        services.AddScoped<CouponRedeemService>();
        services.AddScoped<CharacterQueryService>();
        return services;
    }
}
