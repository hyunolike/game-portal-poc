using Microsoft.EntityFrameworkCore;

namespace GamePortal.Application.Common;

public static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, PageRequest page, CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = total == 0
            ? []
            : await query.Skip(page.Skip).Take(page.Size).ToListAsync(cancellationToken);
        return new PagedResult<T>(items, page.SafePage, page.Size, total);
    }
}
