using GamePortal.Application.Abstractions;
using GamePortal.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace GamePortal.Application.Auditing;

public sealed record AuditLogDto(
    long Id,
    long OperatorId,
    string OperatorName,
    string Action,
    string EntityName,
    string EntityId,
    string? Changes,
    string? IpAddress,
    DateTimeOffset OccurredAt);

public sealed record AuditLogSearch(
    string? EntityName,
    string? EntityId,
    long? OperatorId,
    DateTimeOffset? From,
    DateTimeOffset? To);

public sealed class AuditLogQueryService(IPortalDbContext db)
{
    public Task<PagedResult<AuditLogDto>> SearchAsync(AuditLogSearch search, PageRequest page, CancellationToken cancellationToken)
    {
        var query = db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search.EntityName))
        {
            query = query.Where(a => a.EntityName == search.EntityName);
        }

        if (!string.IsNullOrWhiteSpace(search.EntityId))
        {
            query = query.Where(a => a.EntityId == search.EntityId);
        }

        if (search.OperatorId is not null)
        {
            query = query.Where(a => a.OperatorId == search.OperatorId);
        }

        if (search.From is not null)
        {
            query = query.Where(a => a.OccurredAt >= search.From);
        }

        if (search.To is not null)
        {
            query = query.Where(a => a.OccurredAt < search.To);
        }

        return query
            .OrderByDescending(a => a.Id)
            .Select(a => new AuditLogDto(
                a.Id, a.OperatorId, a.OperatorName, a.Action, a.EntityName, a.EntityId, a.Changes, a.IpAddress, a.OccurredAt))
            .ToPagedResultAsync(page, cancellationToken);
    }
}
