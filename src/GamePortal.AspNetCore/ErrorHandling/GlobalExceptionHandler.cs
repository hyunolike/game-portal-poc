using FluentValidation;
using GamePortal.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GamePortal.AspNetCore.ErrorHandling;

/// <summary>
/// 모든 예외를 RFC 7807 ProblemDetails 로 변환한다.
/// 응답에는 항상 "code"(클라이언트 분기용)와 "traceId"(로그 검색용)가 포함된다.
/// </summary>
internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code, title, errors) = exception switch
        {
            DomainException de => (MapStatus(de.Error.Kind), de.Code, de.Message, null),
            ValidationException ve => (
                StatusCodes.Status400BadRequest,
                "VALIDATION_FAILED",
                "요청 값이 올바르지 않습니다.",
                ve.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
            ArgumentException ae => (StatusCodes.Status400BadRequest, "INVALID_ARGUMENT", ae.Message, null),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "인증이 필요합니다.", null),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested =>
                (StatusCodes.Status499ClientClosedRequest, "CLIENT_CLOSED", "요청이 취소되었습니다.", null),
            _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "일시적인 오류가 발생했습니다. 잠시 후 다시 시도해 주세요.", (Dictionary<string, string[]>?)null),
        };

        if (status >= 500)
        {
            logger.LogError(exception, "Unhandled exception. Path={Path}", httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation("Request failed with {Code}: {Message}", code, exception.Message);
        }

        httpContext.Response.StatusCode = status;
        ProblemDetails body = errors is null
            ? new ProblemDetails()
            : new ValidationProblemDetails(errors);
        body.Status = status;
        body.Title = title;
        body.Extensions["code"] = code;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = body,
            Exception = exception,
        });
    }

    private static int MapStatus(ErrorKind kind) => kind switch
    {
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest,
    };
}
