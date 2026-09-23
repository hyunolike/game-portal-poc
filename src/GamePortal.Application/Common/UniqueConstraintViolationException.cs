namespace GamePortal.Application.Common;

/// <summary>
/// DB UNIQUE 제약 위반. Infrastructure 가 SqlException(2601/2627)을 이 예외로 변환한다.
/// 애플리케이션 계층은 DB 벤더별 에러 번호를 몰라도 된다.
/// </summary>
public sealed class UniqueConstraintViolationException : Exception
{
    public UniqueConstraintViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public UniqueConstraintViolationException()
    {
    }

    public UniqueConstraintViolationException(string message)
        : base(message)
    {
    }
}
