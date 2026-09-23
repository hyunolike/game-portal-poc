using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using GamePortal.Application.Abstractions;
using GamePortal.Domain.Auditing;
using GamePortal.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GamePortal.Infrastructure.Persistence.Interceptors;

/// <summary>
/// 운영툴(Admin.Api)에서만 등록되는 감사 로그 인터셉터.
/// 서비스 코드마다 감사 로그를 직접 남기면 누락이 생기므로, <see cref="IAuditableEntity"/> 변경을
/// SaveChanges 시점에 자동으로 수집해 같은 트랜잭션으로 저장한다.
/// </summary>
internal sealed class AuditSaveChangesInterceptor(ICurrentUser currentUser, TimeProvider clock) : SaveChangesInterceptor
{
    // 한글이 \uXXXX 로 이스케이프되면 운영자가 DB/운영툴에서 바로 읽을 수 없다
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            AddAuditLogs(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            AddAuditLogs(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    private void AddAuditLogs(DbContext context)
    {
        var now = clock.GetUtcNow();
        var entries = context.ChangeTracker.Entries<IAuditableEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var log = AuditLog.Create(
                currentUser.IsAuthenticated ? currentUser.Id : 0,
                currentUser.IsAuthenticated ? currentUser.Name : "system",
                entry.State.ToString(),
                entry.Metadata.ClrType.Name,
                entry.Entity.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                SerializeChanges(entry),
                currentUser.IpAddress,
                now);
            context.Add(log);
        }
    }

    private static string? SerializeChanges(EntityEntry entry)
    {
        var changes = new Dictionary<string, object?>();
        foreach (var property in entry.Properties)
        {
            if (property.Metadata.IsPrimaryKey())
            {
                continue;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    changes[property.Metadata.Name] = property.CurrentValue;
                    break;
                case EntityState.Modified when property.IsModified && !Equals(property.OriginalValue, property.CurrentValue):
                    changes[property.Metadata.Name] = new { before = property.OriginalValue, after = property.CurrentValue };
                    break;
            }
        }

        // 소유 엔티티(예: 쿠폰 보상 JSON)는 생성 시점 값을 함께 남긴다 — 오지급 사고 추적용
        if (entry.State == EntityState.Added)
        {
            foreach (var navigation in entry.Navigations.Where(n => n.Metadata.TargetEntityType.IsOwned()))
            {
                changes[navigation.Metadata.Name] = navigation.CurrentValue;
            }
        }

        return changes.Count == 0 ? null : JsonSerializer.Serialize(changes, JsonOptions);
    }
}
