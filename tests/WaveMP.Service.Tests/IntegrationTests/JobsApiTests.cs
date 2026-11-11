using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using WaveMP.Service.Dtos;
using WaveMP.Service.Tests.IntegrationTests.Extensions;

namespace WaveMP.Service.Tests.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class JobsApiTests
{
    private WaveMpWebApplicationFactory _factory = null!;
    private HttpClient _anonymous = null!;
    private HttpClient _admin = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new WaveMpWebApplicationFactory(
            WaveMpWebApplicationFactory.TestDatabaseName + "_jobs",
            freshDatabase: true);
        _anonymous = _factory.CreateClient();
        _admin = await _factory.CreateAdminClientAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _admin?.Dispose();
        _anonymous?.Dispose();
        await _factory.DeleteDatabaseAsync();
        await _factory.DisposeAsync();
    }

    private static IEnumerable<TestCaseData> Endpoints()
    {
        string runId = Guid.NewGuid().ToString();
        yield return new TestCaseData("GET", "/api/v1/jobs").SetArgDisplayNames("ListJobs");
        yield return new TestCaseData("POST", "/api/v1/jobs/library-scan/runs").SetArgDisplayNames("StartJobRun");
        yield return new TestCaseData("GET", "/api/v1/jobs/library-scan/runs").SetArgDisplayNames("ListJobRuns");
        yield return new TestCaseData("GET", $"/api/v1/jobs/library-scan/runs/{runId}").SetArgDisplayNames("GetJobRun");
        yield return new TestCaseData("GET", "/api/v1/settings/library").SetArgDisplayNames("GetLibrarySettings");
        yield return new TestCaseData("PATCH", "/api/v1/settings/library").SetArgDisplayNames("UpdateLibrarySettings");
    }

    [TestCaseSource(nameof(Endpoints))]
    public async Task AdminEndpoints_WithoutToken_ReturnUnauthorized(string method, string path)
    {
        HttpResponseMessage response = await _anonymous.SendJsonAsync(method, path, method == "PATCH" ? "{}" : null);

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [TestCaseSource(nameof(Endpoints))]
    public async Task AdminEndpoints_AsUser_ReturnForbidden(string method, string path)
    {
        UserDto user = await _admin.CreateUserAsync(new { name = $"User {HttpClientExtensions.Unique()}" });
        using HttpClient client = _factory.CreateAuthorizedClient((await _anonymous.CreateSessionAsync(user.Id)).Token);

        HttpResponseMessage response = await client.SendJsonAsync(method, path, method == "PATCH" ? "{}" : null);

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden, "auth.forbidden");
    }

    [Test]
    public async Task ListJobs_Always_ContainsTheLibraryScanWithItsScheduleAndTheCoversWithout()
    {
        PageDto<JobDto> jobs = await _admin.GetJsonAsync<PageDto<JobDto>>("/api/v1/jobs");

        JobDto scan = jobs.Items.Should().ContainSingle(job => job.Id == JobIdDto.LibraryScan).Subject;
        JobDto covers = jobs.Items.Should().ContainSingle(job => job.Id == JobIdDto.CoverCache).Subject;
        jobs.Total.Should().Be(2);
        scan.State.Should().Be(JobStateDto.Idle);
        scan.IntervalMinutes.Should().Be(360);
        scan.NextRunAt.Should().BeAfter(DateTimeOffset.UtcNow);
        covers.IntervalMinutes.Should().BeNull();
        covers.NextRunAt.Should().BeNull();
    }

    [Test]
    public async Task StartJobRun_CoverCache_PreparesTheCoversManually()
    {
        await _admin.WaitForJobsIdleAsync();

        HttpResponseMessage response = await _admin.PostAsync("/api/v1/jobs/cover-cache/runs", null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted, await response.Content.ReadAsStringAsync());
        JobRunDto run = (await response.Content.ReadFromJsonAsync<JobRunDto>(HttpClientExtensions.JsonOptions))!;
        run.JobId.Should().Be(JobIdDto.CoverCache);
        run.Trigger.Should().Be(JobTriggerDto.Manual);
        JobRunDto finished = await _admin.WaitForJobRunAsync("cover-cache", run.Id);
        finished.Status.Should().Be(JobRunStatusDto.Succeeded);
    }

    [Test]
    public async Task UpdateLibrarySettings_WithNewInterval_PlansTheScanAgainAtOnce()
    {
        try
        {
            HttpResponseMessage response = await _admin.PatchAsJsonAsync(
                "/api/v1/settings/library",
                new { scanIntervalMinutes = 30 },
                HttpClientExtensions.JsonOptions);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            JobDto scan = (await _admin.GetJsonAsync<PageDto<JobDto>>("/api/v1/jobs")).Items
                .Single(job => job.Id == JobIdDto.LibraryScan);

            scan.IntervalMinutes.Should().Be(30);
            scan.NextRunAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(30), TimeSpan.FromMinutes(1));
        }
        finally
        {
            await _admin.PatchAsJsonAsync(
                "/api/v1/settings/library",
                new { scanIntervalMinutes = 360 },
                HttpClientExtensions.JsonOptions);
        }
    }

    [Test]
    public async Task StartJobRun_LibraryScan_IsAcceptedAndRuns()
    {
        HttpResponseMessage response = await _admin.PostAsync("/api/v1/jobs/library-scan/runs", null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        JobRunDto run = (await response.Content.ReadFromJsonAsync<JobRunDto>(HttpClientExtensions.JsonOptions))!;
        response.Headers.Location!.AbsolutePath.Should().Be($"/api/v1/jobs/library-scan/runs/{run.Id}");
        run.JobId.Should().Be(JobIdDto.LibraryScan);
        run.Trigger.Should().Be(JobTriggerDto.Manual);
        ScanDto finished = await _admin.WaitForScanAsync(run.Id);
        finished.Status.Should().Be(JobRunStatusDto.Succeeded);

        JobRunDto stored = await _admin.GetJsonAsync<JobRunDto>($"/api/v1/jobs/library-scan/runs/{run.Id}");
        PageDto<JobRunDto> runs = await _admin.GetJsonAsync<PageDto<JobRunDto>>(
            "/api/v1/jobs/library-scan/runs?status=Succeeded&limit=100");
        JobDto job = (await _admin.GetJsonAsync<PageDto<JobDto>>("/api/v1/jobs")).Items
            .Single(item => item.Id == JobIdDto.LibraryScan);

        stored.Status.Should().Be(JobRunStatusDto.Succeeded);
        stored.DurationMilliseconds.Should().BeGreaterThanOrEqualTo(0);
        runs.Items.Should().Contain(item => item.Id == run.Id);
        job.LastRun!.Id.Should().NotBeEmpty();
    }

    [Test]
    public async Task JobEndpoints_WithUnknownJob_ReturnJobNotFound()
    {
        HttpResponseMessage start = await _admin.PostAsync("/api/v1/jobs/unknown/runs", null);
        HttpResponseMessage list = await _admin.GetAsync("/api/v1/jobs/unknown/runs");
        HttpResponseMessage get = await _admin.GetAsync($"/api/v1/jobs/unknown/runs/{Guid.NewGuid()}");

        await start.ShouldBeProblemAsync(HttpStatusCode.NotFound, "job.not_found");
        await list.ShouldBeProblemAsync(HttpStatusCode.NotFound, "job.not_found");
        await get.ShouldBeProblemAsync(HttpStatusCode.NotFound, "job.not_found");
    }

    [Test]
    public async Task GetJobRun_WithUnknownRun_ReturnsRunNotFound()
    {
        HttpResponseMessage response = await _admin.GetAsync($"/api/v1/jobs/library-scan/runs/{Guid.NewGuid()}");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "job.run_not_found");
    }

    [TestCase("status=9")]
    [TestCase("trigger=Never")]
    [TestCase("offset=-1")]
    public async Task ListJobRuns_WithInvalidQuery_ReturnsBadRequest(string query)
    {
        HttpResponseMessage response = await _admin.GetAsync($"/api/v1/jobs/library-scan/runs?{query}");

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "request.invalid");
    }
}
