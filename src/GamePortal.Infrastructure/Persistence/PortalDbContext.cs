using GamePortal.Application.Abstractions;
using GamePortal.Application.Common;
using GamePortal.Domain.Auditing;
using GamePortal.Domain.Coupons;
using GamePortal.Domain.Notices;
using GamePortal.Domain.Outbox;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GamePortal.Infrastructure.Persistence;

public sealed class PortalDbContext(DbContextOptions<PortalDbContext> options) : DbContext(options), IPortalDbContext
{
    public DbSet<Notice> Notices => Set<Notice>();

    public DbSet<CouponCampaign> CouponCampaigns => Set<CouponCampaign>();

    public DbSet<CouponCode> CouponCodes => Set<CouponCode>();

    public DbSet<CouponRedemption> CouponRedemptions => Set<CouponRedemption>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlErrors.IsUniqueViolation(ex.InnerException))
        {
            throw new UniqueConstraintViolationException(ex.InnerException!.Message, ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PortalDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // 문자열 기본값: nvarchar(max) 방지. 길이 미지정 컬럼은 리뷰에서 걸러지도록 기본 256.
        configurationBuilder.Properties<string>().HaveMaxLength(256);
    }
}

internal static class SqlErrors
{
    // 2601: Cannot insert duplicate key row (unique index), 2627: Violation of UNIQUE KEY constraint
    public static bool IsUniqueViolation(Exception? ex) => ex is SqlException { Number: 2601 or 2627 };
}
