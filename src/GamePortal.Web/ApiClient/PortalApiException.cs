namespace GamePortal.Web.ApiClient;

/// <summary>Web.Api 가 ProblemDetails 로 응답한 오류. 화면 분기는 <see cref="Code"/> 로 한다.</summary>
public sealed class PortalApiException : Exception
{
    public PortalApiException(int statusCode, string code, string? title, string? traceId)
        : base(title ?? code)
    {
        StatusCode = statusCode;
        Code = code;
        TraceId = traceId;
    }

    public PortalApiException()
    {
        Code = "UNKNOWN";
    }

    public PortalApiException(string message)
        : base(message)
    {
        Code = "UNKNOWN";
    }

    public PortalApiException(string message, Exception innerException)
        : base(message, innerException)
    {
        Code = "UNKNOWN";
    }

    public int StatusCode { get; }

    public string Code { get; }

    /// <summary>CS 문의 시 유저에게 보여줄 추적 ID (서버 로그 검색 키)</summary>
    public string? TraceId { get; }
}
