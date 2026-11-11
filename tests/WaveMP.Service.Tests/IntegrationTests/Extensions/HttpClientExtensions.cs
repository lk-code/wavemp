using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Tests.IntegrationTests.Extensions;

/// <summary>
/// Helpers for the API integration tests.
/// </summary>
internal static class HttpClientExtensions
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// A short random text for unique names, e.g. "Lars {unique} Krämer".
    /// </summary>
    public static string Unique()
    {
        return Guid.NewGuid().ToString("N")[..8];
    }

    public static HttpClient CreateAuthorizedClient(this WaveMpWebApplicationFactory factory, string token)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<ProfileDto> GetBuiltInAdminAsync(this HttpClient client)
    {
        PageDto<ProfileDto> page = await client.GetJsonAsync<PageDto<ProfileDto>>("/api/v1/profiles?limit=100");
        return page.Items.Single(profile => profile.IsBuiltIn);
    }

    /// <summary>
    /// Selects the profile and returns the new session with its token.
    /// </summary>
    public static async Task<CreatedSessionDto> CreateSessionAsync(this HttpClient client, Guid profileId)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/sessions",
            new { profileId },
            JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CreatedSessionDto>(JsonOptions))!;
    }

    /// <summary>
    /// Creates a client with a session of the built-in admin profile.
    /// </summary>
    public static async Task<HttpClient> CreateAdminClientAsync(this WaveMpWebApplicationFactory factory)
    {
        using HttpClient anonymous = factory.CreateClient();
        ProfileDto admin = await anonymous.GetBuiltInAdminAsync();
        CreatedSessionDto session = await anonymous.CreateSessionAsync(admin.Id);
        return factory.CreateAuthorizedClient(session.Token);
    }

    /// <summary>
    /// Creates a user through the API of an admin client.
    /// </summary>
    public static async Task<UserDto> CreateUserAsync(this HttpClient adminClient, object request)
    {
        HttpResponseMessage response = await adminClient.PostAsJsonAsync("/api/v1/users", request, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions))!;
    }

    public static async Task<T> GetJsonAsync<T>(this HttpClient client, string path)
    {
        HttpResponseMessage response = await client.GetAsync(path);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;
    }

    /// <summary>
    /// Checks that the response is a Problem Details response with the status and the code.
    /// </summary>
    public static async Task ShouldBeProblemAsync(
        this HttpResponseMessage response,
        HttpStatusCode status,
        string code)
    {
        string content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(status, content);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        JsonNode problem = JsonNode.Parse(content)!;
        problem["code"]!.GetValue<string>().Should().Be(code);
        problem["status"]!.GetValue<int>().Should().Be((int)status);
    }

    /// <summary>
    /// Checks that the response is a validation problem (400, <c>request.invalid</c>) and returns its errors by field.
    /// </summary>
    public static async Task<JsonObject> ShouldBeValidationProblemAsync(this HttpResponseMessage response)
    {
        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "request.invalid");
        JsonNode problem = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        return problem["errors"]!.AsObject();
    }

    /// <summary>
    /// Polls a scan until it has finished (neither queued nor running), then waits until the jobs it started (the
    /// preparation of the covers) have finished as well, and returns the scan. The scan queues those jobs before it
    /// ends, so no job can start unnoticed afterwards. Needs an admin client.
    /// </summary>
    public static async Task<ScanDto> WaitForScanAsync(this HttpClient client, Guid id)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(60));
        while (true)
        {
            ScanDto scan = await client.GetJsonAsync<ScanDto>($"/api/v1/scans/{id}");
            if (scan.Status is not (JobRunStatusDto.Queued or JobRunStatusDto.Running))
            {
                await client.WaitForJobsIdleAsync();
                return scan;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token);
        }
    }

    /// <summary>
    /// Polls the jobs until none is queued or running. Needs an admin client.
    /// </summary>
    public static async Task WaitForJobsIdleAsync(this HttpClient client)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(60));
        while (true)
        {
            PageDto<JobDto> jobs = await client.GetJsonAsync<PageDto<JobDto>>("/api/v1/jobs?limit=100");
            if (jobs.Items.All(job => job.State == JobStateDto.Idle))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token);
        }
    }

    /// <summary>
    /// Polls a run of a job until it has finished and returns it. Needs an admin client.
    /// </summary>
    public static async Task<JobRunDto> WaitForJobRunAsync(this HttpClient client, string jobId, Guid runId)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(60));
        while (true)
        {
            JobRunDto run = await client.GetJsonAsync<JobRunDto>($"/api/v1/jobs/{jobId}/runs/{runId}");
            if (run.Status is not (JobRunStatusDto.Queued or JobRunStatusDto.Running))
            {
                return run;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token);
        }
    }

    /// <summary>
    /// Sends a request with an optional JSON body given as text.
    /// </summary>
    public static async Task<HttpResponseMessage> SendJsonAsync(
        this HttpClient client,
        string method,
        string path,
        string? body = null)
    {
        using HttpRequestMessage request = new(new HttpMethod(method), path);
        if (body is not null)
        {
            request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        }

        return await client.SendAsync(request);
    }
}
