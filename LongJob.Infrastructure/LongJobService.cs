using LongJob.Application.Abstractions;
using LongJob.Domain.Processing;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace LongJob.Infrastructure;

public sealed class LongJobService : ILongJobService
{
    private readonly ConcurrentDictionary<string, string> _payloads = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _ctsMap = new();
    private readonly Random _random = new();
    private readonly ILogger<LongJobService> _logger;

    public LongJobService(ILogger<LongJobService> logger)
    {
        _logger = logger;
    }

    public string StartJob(string input)
    {
        var jobId = Guid.NewGuid().ToString("N");
        var inputLength = input?.Length ?? 0;

        _logger.LogInformation("Starting new job. JobId={JobId}, InputLength={InputLength}", jobId, inputLength);

        var output = TextProcessingService.BuildOutput(input);
        _payloads[jobId] = output;

        var cts = new CancellationTokenSource();
        _ctsMap[jobId] = cts;

        _logger.LogDebug("JobId={JobId} stored. OutputLength={OutputLength}", jobId, output.Length);

        return jobId;
    }

    public async IAsyncEnumerable<char> StreamJobAsync(
        string jobId,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (!_payloads.TryGetValue(jobId, out var output))
        {
            _logger.LogWarning("Stream requested for unknown JobId={JobId}", jobId);
            yield break;
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _ctsMap[jobId].Token);

        _logger.LogInformation("Streaming started for JobId={JobId}, OutputLength={OutputLength}", jobId, output.Length);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        int emitted = 0;
        foreach (var ch in output)
        {
            linked.Token.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(_random.Next(1, 3)), linked.Token);
            emitted++;
            yield return ch;
        }

        sw.Stop();
        _payloads.TryRemove(jobId, out _);
        if (_ctsMap.TryRemove(jobId, out var cts)) cts.Dispose();

        _logger.LogInformation("Streaming completed. JobId={JobId}, DurationMs={ElapsedMs}", jobId, sw.ElapsedMilliseconds);
    }

    public bool CancelJob(string jobId)
    {
        if (_ctsMap.TryRemove(jobId, out var cts))
        {
            _logger.LogWarning("Cancel requested. JobId={JobId}", jobId);

            cts.Cancel();
            cts.Dispose();
            _payloads.TryRemove(jobId, out _);

            _logger.LogInformation("Job canceled successfully. JobId={JobId}", jobId);
            return true;
        }

        _logger.LogWarning("Cancel requested for unknown JobId={JobId}", jobId);
        return false;
    }
}
