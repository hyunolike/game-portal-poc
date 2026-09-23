namespace GamePortal.Domain.Common;

/// <summary>
/// 비즈니스 규칙 위반. API 계층에서 ProblemDetails(code 포함)로 변환된다.
/// 클라이언트(웹/게임 런처)는 message 가 아닌 <see cref="Code"/> 로 분기해야 한다.
/// </summary>
public class DomainException : Exception
{
    public DomainException(DomainError error)
        : base(error.Message)
    {
        Error = error;
    }

    public DomainError Error { get; }

    public string Code => Error.Code;
}

/// <summary>에러 코드 + 기본 메시지. 코드는 외부 계약이므로 변경 시 클라이언트와 협의 필요.</summary>
public sealed record DomainError(string Code, string Message, ErrorKind Kind = ErrorKind.BusinessRule);

public enum ErrorKind
{
    BusinessRule,
    NotFound,
    Conflict,
}
