namespace GamePortal.Application.Abstractions;

/// <summary>
/// 분산 캐시(운영: Redis) 래퍼.
/// Web.Api 와 Admin.Api 는 별도 프로세스이므로, 무효화는 "리전 버전 키"를 올리는 방식으로 한다.
/// (키를 패턴 삭제(SCAN/DEL)하는 방식은 Redis 부하가 커서 대용량 환경에서 지양)
/// </summary>
public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan ttl, CancellationToken cancellationToken);

    Task<string> GetRegionVersionAsync(string region, CancellationToken cancellationToken);

    Task InvalidateRegionAsync(string region, CancellationToken cancellationToken);
}
