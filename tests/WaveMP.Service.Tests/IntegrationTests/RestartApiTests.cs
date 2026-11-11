using System.Net;
using AwesomeAssertions;
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
}
