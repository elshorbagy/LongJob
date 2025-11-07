using LongJob.Application.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace LongJob.Endpoints;

public static class JobEndpoints
{
    public record StartJobRequest([Required, MaxLength(10000)] string Text);
    public record StartJobResponse(string JobId);

    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {        
        app.MapPost("/api/jobs", (StartJobRequest req, ILongJobService svc) =>
        {
            var text = req.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                return Results.BadRequest(new { error = "Text is required and cannot be empty." });

            if (text.Length > 10_000)
                return Results.BadRequest(new { error = "Text too long (max 10,000 characters)." });

            var jobId = svc.StartJob(text);

            return Results.Created($"/api/jobs/{jobId}", new StartJobResponse(jobId));
        });

        app.MapGet("/api/jobs/{jobId}/stream", async (HttpContext ctx, string jobId, ILongJobService svc) =>
        {
            if (!IsValidJobId(jobId))
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                await ctx.Response.WriteAsync("Invalid jobId format.");
                return;
            }

            ctx.Response.Headers.Append("Content-Type", "text/event-stream");
            ctx.Response.Headers.Append("Cache-Control", "no-cache");
            ctx.Response.Headers.Append("X-Accel-Buffering", "no");

            await foreach (var ch in svc.StreamJobAsync(jobId, ctx.RequestAborted))
            {
                await ctx.Response.WriteAsync($"data: {ch}\n\n", ctx.RequestAborted);
                await ctx.Response.Body.FlushAsync(ctx.RequestAborted);
            }
        });

        app.MapDelete("/api/jobs/{jobId}", (string jobId, ILongJobService svc) =>
        {
            if (!IsValidJobId(jobId))
                return Results.BadRequest(new { error = "Invalid jobId format." });

            var ok = svc.CancelJob(jobId);
            return ok ? Results.NoContent() : Results.NotFound(new { error = "Job not found or already completed." });
        });

        return app;
    }

    private static bool IsValidJobId(string jobId)
    {
        return !string.IsNullOrWhiteSpace(jobId)
               && jobId.Length == 32
               && jobId.All(c => char.IsDigit(c) || (c >= 'a' && c <= 'f'));
    }
}
