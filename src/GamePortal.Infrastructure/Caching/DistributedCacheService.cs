using System.Text.Json;
using GamePortal.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace GamePortal.Infrastructure.Caching;

/// <summary>
/// IDistributedCache(Redis) 기반 캐시.
/// - 캐시는 원본이 아니다: Redis 장애 시 예외를 삼키고 DB 로 폴백(fail-open)해 서비스 전체 장애로 번지지 않게 한다.
/// - null 결과도 캐싱(negative caching)해 없는 키 반복 조회로 DB 가 맞는 것을 막는다.
/// </summary>
internal sealed class DistributedCacheService(IDistributedCache cache, ILogger<DistributedCacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan ttl, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await cache.GetAsync(key, cancellationToken);
            if (bytes is not null)
            {
                var entry = JsonSerializer.Deserialize<CacheEntry<T>>(bytes, JsonOptions);
                if (entry is not null)
                {
                    return entry.Value;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Cache read failed. Key={CacheKey}", key);
        }

        var value = await factory(cancellationToken);

        try
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(new CacheEntry<T>(value), JsonOptions);
            await cache.SetAsync(key, payload, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Cache write failed. Key={CacheKey}", key);
        }

        return value;
    }

    public async Task<string> GetRegionVersionAsync(string region, CancellationToken cancellationToken)
    {
        try
        {
            var version = await cache.GetStringAsync(VersionKey(region), cancellationToken);
            if (version is not null)
            {
                return version;
            }

            version = NewVersion();
            await cache.SetStringAsync(VersionKey(region), version, cancellationToken);
            return version;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Cache version read failed. Region={Region}", region);
            return "fallback";
        }
    }

    public async Task InvalidateRegionAsync(string region, CancellationToken cancellationToken)
    {
        try
        {
            // 이전 버전 키들은 TTL 로 자연 만료된다.
            await cache.SetStringAsync(VersionKey(region), NewVersion(), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 무효화 실패는 최대 TTL 동안 구버전이 노출될 수 있으므로 Error 로 남겨 알람 대상으로 둔다.
            logger.LogError(ex, "Cache invalidation failed. Region={Region}", region);
        }
    }

    private static string VersionKey(string region) => $"{region}:__version";

    private static string NewVersion() => Guid.NewGuid().ToString("N")[..12];

    private sealed record CacheEntry<T>(T Value);
}
