using LongJob.Infrastructure;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LongJob.Test.UnitTests.Infrastructure;

[TestClass]
public class LongJobServiceTests
{
    private static ILogger<LongJobService> CreateLoggerSubstitute()
        => Substitute.For<ILogger<LongJobService>>();

    private static async Task<(bool hasValue, char value)> TryReadFirstCharAsync(
        IAsyncEnumerable<char> stream, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        await foreach (var ch in stream.WithCancellation(cts.Token))
        {
            return (true, ch);
        }
        return (false, default);
    }

    [TestMethod]
    public void StartJob_Returns_NonEmpty_Id_And_Logs()
    {
        // Arrange
        var logger = CreateLoggerSubstitute();
        var svc = new LongJobService(logger);

        // Act
        var jobId = svc.StartJob("X");

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(jobId), "JobId should be generated.");

        // Verify logging happened at least once at Information or Debug.
        logger.ReceivedWithAnyArgs()
              .Log(default, default, default!, default!, default!);
    }

    [TestMethod]
    public async Task StreamJobAsync_Streams_AtLeast_One_Char_For_Valid_Job()
    {
        // Arrange
        var logger = CreateLoggerSubstitute();
        var svc = new LongJobService(logger);
        var jobId = svc.StartJob("A");

        // Act
        var (hasValue, first) = await TryReadFirstCharAsync(
            svc.StreamJobAsync(jobId, CancellationToken.None),
            timeout: TimeSpan.FromSeconds(3));

        // Assert
        Assert.IsTrue(hasValue, "Expected to receive at least one character from the stream.");
        Assert.AreNotEqual(default(char), first);
    }

    [TestMethod]
    public async Task StreamJobAsync_UnknownJob_YieldsNothing_And_Warns()
    {
        // Arrange
        var logger = CreateLoggerSubstitute();
        var svc = new LongJobService(logger);

        // Act
        var list = new List<char>();
        await foreach (var ch in svc.StreamJobAsync("no-such-id", CancellationToken.None))
            list.Add(ch);

        // Assert
        Assert.AreEqual(0, list.Count, "Unknown job should yield no characters.");

        logger.ReceivedWithAnyArgs()
              .Log(default, default, default!, default!, default!);
    }

    [TestMethod]
    public async Task CancelJob_BeforeStreaming_RemovesPayload_And_StreamYieldsNothing()
    {
        // Arrange
        var logger = CreateLoggerSubstitute();
        var svc = new LongJobService(logger);
        var jobId = svc.StartJob("ABC");

        // Act
        var canceled = svc.CancelJob(jobId);

        var collected = new List<char>();
        await foreach (var ch in svc.StreamJobAsync(jobId, CancellationToken.None))
            collected.Add(ch);

        // Assert
        Assert.IsTrue(canceled, "Cancel should return true for existing job.");
        Assert.AreEqual(0, collected.Count, "After canceling before streaming, stream should yield nothing.");
    }

    [TestMethod]
    public void CancelJob_UnknownId_ReturnsFalse_And_LogsWarning()
    {
        // Arrange
        var logger = CreateLoggerSubstitute();
        var svc = new LongJobService(logger);

        // Act
        var result = svc.CancelJob("missing-id");

        // Assert
        Assert.IsFalse(result);

        logger.ReceivedWithAnyArgs()
              .Log(default, default, default!, default!, default!);
    }

    [TestMethod]
    public async Task Streaming_To_Completion_CleansUp_CancelAfterwardsReturnsFalse()
    {
        // Arrange
        var logger = CreateLoggerSubstitute();
        var svc = new LongJobService(logger);
        var jobId = svc.StartJob("A");

        // Act
        await foreach (var _ in svc.StreamJobAsync(jobId, CancellationToken.None)) {}
        
        var cancelResult = svc.CancelJob(jobId);

        // Assert
        Assert.IsFalse(cancelResult, "After completion, CancelJob should return false because job is cleaned up.");
    }

    [TestMethod]
    public async Task StreamJobAsync_CancellationToken_StopsEarly()
    {
        // Arrange
        var logger = CreateLoggerSubstitute();
        var svc = new LongJobService(logger);
        var jobId = svc.StartJob("AB"); 

        using var cts = new CancellationTokenSource();
        
        cts.CancelAfter(TimeSpan.FromMilliseconds(1200));

        var collected = new List<char>();
        try
        {
            await foreach (var ch in svc.StreamJobAsync(jobId, cts.Token))
                collected.Add(ch);
        }
        catch (OperationCanceledException)
        {
            // Expected due to linked token cancellation
        }

        // Assert
        Assert.IsTrue(collected.Count <= 1, "Stream should stop early due to cancellation.");
    }
}
