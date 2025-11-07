using System.Net;
using System.Text.Json;

namespace LongJob.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;
    private const int ClientClosedRequestStatusCode = 499;

    public async Task InvokeAsync(HttpContext context)
    {
        var jobId = context.Request.RouteValues["jobId"]?.ToString() ?? "N/A";

        try
        {
            await _next(context);
        }
        catch (TaskCanceledException ex)
        {
            var message = "Request was cancelled by the client.";
            _logger.LogError(ex, "{message} Job Id {JobId}.", message, jobId);

            context.Response.StatusCode = ClientClosedRequestStatusCode;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = message,
                code = ClientClosedRequestStatusCode
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while processing request. Job Id {JobId}.", jobId);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = ex.Message,
                stackTrace = ex.StackTrace,
                code = context.Response.StatusCode
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
