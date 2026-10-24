using Microsoft.AspNetCore.Diagnostics;
using System.Diagnostics;

namespace LogiTrack.Shared.ErrorHandling;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.TraceId;
        
        logger.LogError(exception, "An error occurred while processing the request. Trace ID: {TraceId}", traceId);
        
        await Results.Problem(
            title: "An error occurred while processing your request.",
            detail: exception.Message,
            statusCode: StatusCodes.Status500InternalServerError,
            extensions: new Dictionary<string, object?>
            {
                { "traceId", traceId.ToString() }
            }
        ).ExecuteAsync(httpContext);

        return true;
    }
}