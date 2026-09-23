using GamePortal.Domain.Auditing;
using GamePortal.Domain.Coupons;
using GamePortal.Domain.Notices;
using GamePortal.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace GamePortal.Application.Abstractions;

/// <summary>
/// 애플리케이션 계층이 사용하는 DbContext 추상화.
/// Repository 패턴을 한 번 더 감싸지 않고 EF Core(IQueryable)를 그대로 사용한다.
/// (EF Core 자체가 Repository + UoW 이며, 과도한 추상화는 쿼리 최적화를 어렵게 만든다)
/// </summary>
public interface IPortalDbContext
{
    DbSet<Notice> Notices { get; }

    DbSet<CouponCampaign> CouponCampaigns { get; }

    DbSet<CouponCode> CouponCodes { get; }

    DbSet<CouponRedemption> CouponRedemptions { get; }

    DbSet<OutboxMessage> OutboxMessages { get; }

    DbSet<AuditLog> AuditLogs { get; }

    DatabaseFacade Database { get; }

    ChangeTracker ChangeTracker { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
