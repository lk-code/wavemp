using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using WaveMP.Core.Jobs.Definitions;
using WaveMP.Data;
using WaveMP.Data.Entities;
using WaveMP.Service.Dtos;
using WaveMP.Service.Tests.IntegrationTests.Extensions;

namespace WaveMP.Service.Tests.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class RestartApiTests
{
    private const string DatabaseName = WaveMpWebApplicationFactory.TestDatabaseName + "_restart";

    [Test]
    public async Task Restart_WithExistingDatabase_KeepsSessionsAndTheSingleBuiltInAdmin()
    {
        string token;
        await using (WaveMpWebApplicationFactory first = new(DatabaseName, freshDatabase: true))
        {
            using HttpClient client = first.CreateClient();
            ProfileDto admin = await client.GetBuiltInAdminAsync();
            token = (await client.CreateSessionAsync(admin.Id)).Token;
        }

        await using WaveMpWebApplicationFactory second = new(DatabaseName);
        using HttpClient anonymous = second.CreateClient();
        using HttpClient authorized = second.CreateAuthorizedClient(token);

        HttpResponseMessage response = await authorized.GetAsync("/api/v1/sessions/current");
        PageDto<ProfileDto> profiles = await anonymous.GetJsonAsync<PageDto<ProfileDto>>("/api/v1/profiles");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "the signing key is kept in the database");
        profiles.Items.Should().ContainSingle(profile => profile.IsBuiltIn);
        profiles.Total.Should().Be(1);
        await second.DeleteDatabaseAsync();
    }

    [Test]
    public async Task Restart_WithRunThatWasRunning_MarksItAsInterrupted()
    {
        var runId = Guid.CreateVersion7();
        await using (WaveMpWebApplicationFactory first = new(DatabaseName + "_jobs", freshDatabase: true))
        {
            await using AsyncServiceScope scope = first.Services.CreateAsyncScope();
            WaveMpDbContext db = scope.ServiceProvider.GetRequiredService<WaveMpDbContext>();
            db.JobRuns.Add(new JobRun
            {
                Id = runId,
                JobId = JobIds.LibraryScan,
                Trigger = JobRunTrigger.Manual,
                Status = JobRunStatus.Running,
                QueuedAt = DateTimeOffset.UtcNow,
                StartedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        await using WaveMpWebApplicationFactory second = new(DatabaseName + "_jobs");
        using HttpClient admin = await second.CreateAdminClientAsync();

        JobRunDto run = await admin.GetJsonAsync<JobRunDto>($"/api/v1/jobs/library-scan/runs/{runId}");
        ScanDto scan = await admin.GetJsonAsync<ScanDto>($"/api/v1/scans/{runId}");

        run.Status.Should().Be(JobRunStatusDto.Interrupted);
        run.ErrorCode.Should().Be(JobErrorCodeDto.JobInterrupted);
        run.FinishedAt.Should().NotBeNull();
        scan.Status.Should().Be(JobRunStatusDto.Interrupted);
        await second.DeleteDatabaseAsync();
    }
}
