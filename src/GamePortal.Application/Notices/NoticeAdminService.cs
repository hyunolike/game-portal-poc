using FluentValidation;
using GamePortal.Application.Abstractions;
using GamePortal.Application.Common;
using GamePortal.Domain.Common;
using GamePortal.Domain.Notices;
using Microsoft.EntityFrameworkCore;

namespace GamePortal.Application.Notices;

/// <summary>운영툴 공지 관리. 모든 변경은 AuditLog 에 자동 기록된다(Infrastructure 인터셉터).</summary>
public sealed class NoticeAdminService(
    IPortalDbContext db,
    ICacheService cache,
    ICurrentUser currentUser,
    TimeProvider clock,
    IValidator<CreateNoticeRequest> createValidator,
    IValidator<UpdateNoticeRequest> updateValidator)
{
    public Task<PagedResult<AdminNoticeDto>> GetListAsync(NoticeCategory? category, PageRequest page, CancellationToken cancellationToken)
    {
        var query = db.Notices.AsNoTracking().Where(n => !n.IsDeleted);
        if (category is not null)
        {
            query = query.Where(n => n.Category == category);
        }

        return query
            .OrderByDescending(n => n.Id)
            .Select(ToDto)
            .ToPagedResultAsync(page, cancellationToken);
    }

    public async Task<AdminNoticeDto> GetAsync(long id, CancellationToken cancellationToken)
    {
        return await db.Notices.AsNoTracking()
                   .Where(n => n.Id == id && !n.IsDeleted)
                   .Select(ToDto)
                   .SingleOrDefaultAsync(cancellationToken)
               ?? throw new DomainException(NoticeErrors.NotFound);
    }

    public async Task<long> CreateAsync(CreateNoticeRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var now = clock.GetUtcNow();

        var notice = Notice.Create(
            request.Category,
            request.Title,
            request.Content,
            request.IsPinned,
            request.PublishAt ?? now,
            currentUser.Id,
            now);

        db.Notices.Add(notice);
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateRegionAsync(NoticeQueryService.CacheRegion, cancellationToken);
        return notice.Id;
    }

    public async Task UpdateAsync(long id, UpdateNoticeRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var notice = await FindAsync(id, cancellationToken);

        notice.Update(
            request.Category,
            request.Title,
            request.Content,
            request.IsPinned,
            request.IsPublished,
            request.PublishAt,
            currentUser.Id,
            clock.GetUtcNow());

        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateRegionAsync(NoticeQueryService.CacheRegion, cancellationToken);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var notice = await FindAsync(id, cancellationToken);
        notice.Delete(currentUser.Id, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateRegionAsync(NoticeQueryService.CacheRegion, cancellationToken);
    }

    private async Task<Notice> FindAsync(long id, CancellationToken cancellationToken)
    {
        var notice = await db.Notices.SingleOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (notice is null || notice.IsDeleted)
        {
            throw new DomainException(NoticeErrors.NotFound);
        }

        return notice;
    }

    private static readonly System.Linq.Expressions.Expression<Func<Notice, AdminNoticeDto>> ToDto = n => new AdminNoticeDto(
        n.Id, n.Category, n.Title, n.Content, n.IsPinned, n.IsPublished, n.PublishAt, n.CreatedBy, n.CreatedAt, n.UpdatedBy, n.UpdatedAt);
}
