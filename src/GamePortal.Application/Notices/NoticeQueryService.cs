using GamePortal.Application.Abstractions;
using GamePortal.Application.Common;
using GamePortal.Domain.Common;
using GamePortal.Domain.Notices;
using Microsoft.EntityFrameworkCore;

namespace GamePortal.Application.Notices;

/// <summary>
/// 웹사이트(유저) 공지 조회. 트래픽의 대부분이 읽기이므로 분산 캐시를 적용한다.
/// 점검/업데이트 공지 시점에 트래픽이 수십 배로 몰리는 게임 특성상 DB 직접 조회는 피한다.
/// </summary>
public sealed class NoticeQueryService(IPortalDbContext db, ICacheService cache, TimeProvider clock)
{
    internal const string CacheRegion = "notices";

    // 예약 공지는 최대 TTL 만큼 늦게 노출될 수 있다. (운영 합의: 1분 이내 허용)
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    public async Task<PagedResult<NoticeSummaryDto>> GetListAsync(NoticeCategory? category, PageRequest page, CancellationToken cancellationToken)
    {
        var version = await cache.GetRegionVersionAsync(CacheRegion, cancellationToken);
        var key = $"{CacheRegion}:{version}:list:{category?.ToString() ?? "all"}:{page.SafePage}:{page.Size}";

        return await cache.GetOrCreateAsync(
            key,
            ct =>
            {
                var now = clock.GetUtcNow();
                var query = db.Notices.AsNoTracking()
                    .Where(n => !n.IsDeleted && n.IsPublished && n.PublishAt <= now);

                if (category is not null)
                {
                    query = query.Where(n => n.Category == category);
                }

                return query
                    .OrderByDescending(n => n.IsPinned)
                    .ThenByDescending(n => n.PublishAt)
                    .ThenByDescending(n => n.Id)
                    .Select(n => new NoticeSummaryDto(n.Id, n.Category, n.Title, n.IsPinned, n.PublishAt))
                    .ToPagedResultAsync(page, ct);
            },
            CacheTtl,
            cancellationToken);
    }

    public async Task<NoticeDetailDto> GetAsync(long id, CancellationToken cancellationToken)
    {
        var version = await cache.GetRegionVersionAsync(CacheRegion, cancellationToken);
        var key = $"{CacheRegion}:{version}:detail:{id}";

        // 존재하지 않는 ID 는 null 로 캐싱해 반복 조회(크롤러 등)로 DB 를 두드리지 않게 한다.
        var dto = await cache.GetOrCreateAsync(
            key,
            async ct =>
            {
                var now = clock.GetUtcNow();
                return await db.Notices.AsNoTracking()
                    .Where(n => n.Id == id && !n.IsDeleted && n.IsPublished && n.PublishAt <= now)
                    .Select(n => new NoticeDetailDto(n.Id, n.Category, n.Title, n.Content, n.IsPinned, n.PublishAt))
                    .SingleOrDefaultAsync(ct);
            },
            CacheTtl,
            cancellationToken);

        return dto ?? throw new DomainException(NoticeErrors.NotFound);
    }
}
