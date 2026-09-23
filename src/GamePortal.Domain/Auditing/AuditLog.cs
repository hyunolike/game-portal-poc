namespace GamePortal.Domain.Auditing;

/// <summary>
/// 운영툴 감사 로그. "누가, 언제, 무엇을, 어떻게 바꿨는가".
/// 게임 운영 사고(오지급, 잘못된 공지) 발생 시 추적 근거가 되므로 수정/삭제 API 를 제공하지 않는다.
/// </summary>
public class AuditLog
{
    private AuditLog()
    {
        OperatorName = string.Empty;
        Action = string.Empty;
        EntityName = string.Empty;
        EntityId = string.Empty;
    }

    public long Id { get; private set; }

    public long OperatorId { get; private set; }

    public string OperatorName { get; private set; }

    /// <summary>Added / Modified / Deleted</summary>
    public string Action { get; private set; }

    public string EntityName { get; private set; }

    public string EntityId { get; private set; }

    /// <summary>변경 컬럼의 before/after JSON.</summary>
    public string? Changes { get; private set; }

    public string? IpAddress { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static AuditLog Create(
        long operatorId,
        string operatorName,
        string action,
        string entityName,
        string entityId,
        string? changes,
        string? ipAddress,
        DateTimeOffset now) => new()
        {
            OperatorId = operatorId,
            OperatorName = operatorName,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Changes = changes,
            IpAddress = ipAddress,
            OccurredAt = now,
        };
}
