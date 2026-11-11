using System.Net;
using AwesomeAssertions;
using WaveMP.Service.Dtos;
using WaveMP.Service.Tests.IntegrationTests.Extensions;

namespace WaveMP.Service.Tests.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class ProfilesApiTests
{
    private const string DatabaseName = WaveMpWebApplicationFactory.TestDatabaseName + "_profiles";

    private WaveMpWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private HttpClient _admin = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new WaveMpWebApplicationFactory(DatabaseName, freshDatabase: true);
        _client = _factory.CreateClient();
        _admin = await _factory.CreateAdminClientAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _admin?.Dispose();
        _client?.Dispose();
        await _factory.DeleteDatabaseAsync();
        await _factory.DisposeAsync();
    }

    [Test]
    public async Task ListProfiles_OnNewInstallation_ReturnsOnlyTheBuiltInAdmin()
    {
        await using WaveMpWebApplicationFactory factory = new(DatabaseName + "_new", freshDatabase: true);
        using HttpClient client = factory.CreateClient();

        PageDto<ProfileDto> page = await client.GetJsonAsync<PageDto<ProfileDto>>("/api/v1/profiles");

        page.Total.Should().Be(1);
        page.Offset.Should().Be(0);
        page.Limit.Should().Be(50);
        ProfileDto admin = page.Items.Should().ContainSingle().Subject;
        admin.Name.Should().Be("Admin");
        admin.Initials.Should().Be("A");
        admin.Color.Should().Be("#1f6fd6");
        admin.Role.Should().Be(RoleDto.Admin);
        admin.IsBuiltIn.Should().BeTrue();
        await factory.DeleteDatabaseAsync();
    }

    [Test]
    public async Task ListProfiles_WithoutToken_IsAvailable()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/v1/profiles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Test]
    public async Task ListProfiles_WithPaging_ReturnsThePageInCreationOrderWithTheBuiltInAdminLast()
    {
        UserDto first = await _admin.CreateUserAsync(new { name = $"First {HttpClientExtensions.Unique()}" });
        UserDto second = await _admin.CreateUserAsync(new { name = $"Second {HttpClientExtensions.Unique()}" });
        PageDto<ProfileDto> all = await _client.GetJsonAsync<PageDto<ProfileDto>>("/api/v1/profiles?limit=100");
        List<Guid> ids = [.. all.Items.Select(profile => profile.Id)];

        PageDto<ProfileDto> page = await _client.GetJsonAsync<PageDto<ProfileDto>>(
            $"/api/v1/profiles?offset={ids.IndexOf(first.Id)}&limit=1");

        all.Total.Should().Be(all.Items.Count);
        all.Items[^1].IsBuiltIn.Should().BeTrue();
        ids.IndexOf(first.Id).Should().BeLessThan(ids.IndexOf(second.Id));
        page.Items.Should().ContainSingle().Which.Id.Should().Be(first.Id);
        page.Limit.Should().Be(1);
        page.Total.Should().Be(all.Total);
    }

    [TestCase("limit=0")]
    [TestCase("limit=101")]
    [TestCase("offset=-1")]
    [TestCase("limit=abc")]
    public async Task ListProfiles_WithInvalidPaging_ReturnsBadRequest(string query)
    {
        HttpResponseMessage response = await _client.GetAsync($"/api/v1/profiles?{query}");

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "request.invalid");
    }
}
