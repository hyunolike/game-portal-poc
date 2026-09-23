using FluentValidation;
using GamePortal.Application.Auditing;
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
        services.AddScoped<CouponRedeemService>();
        services.AddScoped<CouponCampaignAdminService>();
        services.AddScoped<OutboxAdminService>();
        services.AddScoped<AuditLogQueryService>();

        return services;
    }
}
