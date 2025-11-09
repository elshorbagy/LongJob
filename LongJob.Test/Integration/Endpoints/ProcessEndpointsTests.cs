using LongJob.Application.Abstractions;
using LongJob.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using System.Net;
using System.Text;
using System.Text.Json;

namespace LongJob.Test.Integration.Endpoints;

[TestClass]
public class ProcessEndpointsTests
{
    private static async Task<(IHost host, HttpClient client, ILongJobService svc)> CreateHostAsync(ILongJobService svc = null!)
    {
        svc ??= Substitute.For<ILongJobService>();

        var builder = Host.CreateDefaultBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddSingleton(svc);
                });

                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        JobEndpoints.MapJobEndpoints(endpoints);
                    });
                });
            });

        var host = await builder.StartAsync();
        var client = host.GetTestClient();
        return (host, client, svc);
    }


    private static StringContent Json(object o) =>
        new StringContent(JsonSerializer.Serialize(o), Encoding.UTF8, "application/json");

    private static string ValidJobId => new('a', 32); // 32 lowercase hex chars

    private static async IAsyncEnumerable<char> ToAsyncChars(string s, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken _ = default)
    {
        foreach (var c in s)
        {
            await Task.Yield();
            yield return c;
        }
    }

    [TestMethod]
    public async Task Post_Jobs_Returns201_With_Location_And_Body()
    {
        // Arrange
        var (host, client, svc) = await CreateHostAsync();
        var jobId = ValidJobId;
        svc.StartJob(Arg.Any<string>()).Returns(jobId);

        // Act
        var resp = await client.PostAsync("/api/jobs", Json(new { text = "  hello  " }));

        // Assert
        Assert.AreEqual(HttpStatusCode.Created, resp.StatusCode);

        // Location header must point to resource
        var location = resp.Headers.Location?.ToString();
        Assert.IsNotNull(location);
        StringAssert.Contains(location, $"/api/jobs/{jobId}");

        var body = await resp.Content.ReadAsStringAsync();
        Assert.IsTrue(body.Contains(jobId));

        // StartJob called with trimmed text
        svc.Received(1).StartJob(Arg.Is<string>(s => s == "hello"));

        await host.StopAsync();
    }

    [TestMethod]
    public async Task Post_Jobs_EmptyOrWhitespace_Returns400_With_Error()
    {
        var (host, client, _) = await CreateHostAsync();

        var resp = await client.PostAsync("/api/jobs", Json(new { text = "   " }));
        Assert.AreEqual(HttpStatusCode.BadRequest, resp.StatusCode);

        var json = await resp.Content.ReadAsStringAsync();
        StringAssert.Contains(json, "Text is required");

        await host.StopAsync();
    }

    [TestMethod]
    public async Task Post_Jobs_TooLong_Returns400_With_Error()
    {
        var (host, client, _) = await CreateHostAsync();
        var veryLong = new string('x', 10001);

        var resp = await client.PostAsync("/api/jobs", Json(new { text = veryLong }));
        Assert.AreEqual(HttpStatusCode.BadRequest, resp.StatusCode);

        var json = await resp.Content.ReadAsStringAsync();
        StringAssert.Contains(json, "Text too long");

        await host.StopAsync();
    }

    [TestMethod]
    public async Task Get_Stream_InvalidJobId_Returns400_TextPlain()
    {
        var (host, client, _) = await CreateHostAsync();

        var resp = await client.GetAsync("/api/jobs/not-valid-id/stream");
        Assert.AreEqual(HttpStatusCode.BadRequest, resp.StatusCode);

        var text = await resp.Content.ReadAsStringAsync();
        StringAssert.Contains(text, "Invalid jobId format");

        await host.StopAsync();
    }

    [TestMethod]
    public async Task Get_Stream_ValidJobId_Sends_Data_Lines()
    {
        // Arrange
        var (host, client, svc) = await CreateHostAsync();

        svc.StreamJobAsync(ValidJobId, Arg.Any<CancellationToken>())
           .Returns(ci => ToAsyncChars("Hi", ci.Arg<CancellationToken>()));

        // Act
        var resp = await client.GetAsync($"/api/jobs/{ValidJobId}/stream", HttpCompletionOption.ResponseHeadersRead);

        // Assert headers
        Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
        Assert.AreEqual("text/event-stream", resp.Content.Headers.ContentType?.MediaType);

        var body = await resp.Content.ReadAsStringAsync();
        
        StringAssert.Contains(body, "data: H");
        StringAssert.Contains(body, "data: i");

        await host.StopAsync();
    }

    [TestMethod]
    public async Task Delete_InvalidJobId_Returns400()
    {
        var (host, client, _) = await CreateHostAsync();

        var resp = await client.DeleteAsync("/api/jobs/badid");
        Assert.AreEqual(HttpStatusCode.BadRequest, resp.StatusCode);

        await host.StopAsync();
    }

    [TestMethod]
    public async Task Delete_ValidJobId_Found_Returns204()
    {
        var (host, client, svc) = await CreateHostAsync();
        svc.CancelJob(ValidJobId).Returns(true);

        var resp = await client.DeleteAsync($"/api/jobs/{ValidJobId}");
        Assert.AreEqual(HttpStatusCode.NoContent, resp.StatusCode);

        svc.Received(1).CancelJob(Arg.Is<string>(s => s == ValidJobId));
        await host.StopAsync();
    }

    [TestMethod]
    public async Task Delete_ValidJobId_NotFound_Returns404_With_Error()
    {
        var (host, client, svc) = await CreateHostAsync();
        svc.CancelJob(ValidJobId).Returns(false);

        var resp = await client.DeleteAsync($"/api/jobs/{ValidJobId}");
        Assert.AreEqual(HttpStatusCode.NotFound, resp.StatusCode);

        var text = await resp.Content.ReadAsStringAsync();
        StringAssert.Contains(text, "Job not found");

        svc.Received(1).CancelJob(Arg.Is<string>(s => s == ValidJobId));
        await host.StopAsync();
    }
}
