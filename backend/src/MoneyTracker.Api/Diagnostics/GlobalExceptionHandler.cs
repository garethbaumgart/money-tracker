using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MoneyTracker.Api.Observability;

namespace MoneyTracker.Api.Diagnostics;

internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException or TaskCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation(
                exception,
                "Request was canceled while processing {Method} {Path} correlationId={CorrelationId} traceId={TraceId} errorCode={ErrorCode}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                CorrelationHeaders.GetCorrelationId(httpContext),
                CorrelationHeaders.GetTraceId(httpContext),
                ApiErrorCodes.OperationCanceled);

            return true;
        }

        logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path} correlationId={CorrelationId} traceId={TraceId} errorCode={ErrorCode}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            CorrelationHeaders.GetCorrelationId(httpContext),
            CorrelationHeaders.GetTraceId(httpContext),
            ApiErrorCodes.InternalServerError);

        if (httpContext.Response.HasStarted)
        {
            logger.LogWarning(
                "Unable to write ProblemDetails because the response has already started for {Method} {Path} correlationId={CorrelationId} traceId={TraceId} errorCode={ErrorCode}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                CorrelationHeaders.GetCorrelationId(httpContext),
                CorrelationHeaders.GetTraceId(httpContext),
                ApiErrorCodes.InternalServerError);
            return false;
        }

        var problemDetails = UnhandledExceptionProblemDetailsFactory.Create(httpContext);
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.Headers[CorrelationHeaders.CorrelationIdHeader] =
            CorrelationHeaders.GetCorrelationId(httpContext);

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });

        return true;
    }
}
