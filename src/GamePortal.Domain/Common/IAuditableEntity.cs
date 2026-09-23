namespace GamePortal.Domain.Common;

/// <summary>운영툴에서 변경 시 감사 로그(AuditLog)를 남겨야 하는 엔티티 표식.</summary>
public interface IAuditableEntity
{
    long Id { get; }
}
