namespace GamePortal.Application.Abstractions;

/// <summary>
/// 고유 쿠폰 코드 대량 INSERT. EF 의 SaveChanges 로 10만 건을 넣으면 수십 초가 걸리므로
/// SqlBulkCopy 로 구현한다. 현재 DbContext 트랜잭션에 참여한다.
/// </summary>
public interface ICouponCodeBulkWriter
{
    Task WriteAsync(long campaignId, IReadOnlyCollection<string> codes, CancellationToken cancellationToken);
}
