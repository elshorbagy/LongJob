namespace LongJob.Application.Abstractions;

public interface ILongJobService
{
    string StartJob(string input);
    IAsyncEnumerable<char> StreamJobAsync(string jobId, CancellationToken ct);
    bool CancelJob(string jobId);
}
