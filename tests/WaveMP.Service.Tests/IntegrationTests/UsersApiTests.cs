using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using WaveMP.Service.Dtos;
using WaveMP.Service.Tests.IntegrationTests.Extensions;

namespace WaveMP.Service.Tests.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class UsersApiTests
{
    private static readonly string[] _palette =
    [
        "#1f6fd6",
        "#0b7a8a",
        "#2e7d32",
        "#6a3fb5",
        "#c2185b",
        "#c4441c",
        "#8a5a00",
        "#4a5a6a",
    ];

    private WaveMpWebApplicationFactory _factory = null!;
    private HttpClient _anonymous = null!;
    private HttpClient _admin = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new WaveMpWebApplicationFactory(
            WaveMpWebApplicationFactory.TestDatabaseName + "_users",
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
        string id = Guid.NewGuid().ToString();
        yield return new TestCaseData("GET", "/api/v1/users", null).SetArgDisplayNames("ListUsers");
        yield return new TestCaseData("GET", $"/api/v1/users/{id}", null).SetArgDisplayNames("GetUser");
        yield return new TestCaseData("POST", "/api/v1/users", "{\"name\":\"Nobody\"}").SetArgDisplayNames("CreateUser");
        yield return new TestCaseData("PATCH", $"/api/v1/users/{id}", "{}").SetArgDisplayNames("UpdateUser");
        yield return new TestCaseData("DELETE", $"/api/v1/users/{id}", null).SetArgDisplayNames("DeleteUser");
    }

    [TestCaseSource(nameof(Endpoints))]
    public async Task UserEndpoints_WithoutToken_ReturnUnauthorized(string method, string path, string? body)
    {
        HttpResponseMessage response = await SendAsync(_anonymous, method, path, body);

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [TestCaseSource(nameof(Endpoints))]
    public async Task UserEndpoints_AsUser_ReturnForbidden(string method, string path, string? body)
    {
        using HttpClient user = await CreateUserClientAsync();

        HttpResponseMessage response = await SendAsync(user, method, path, body);

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden, "auth.forbidden");
    }

    [Test]
    public async Task CreateUser_WithName_DerivesInitialsColorAndRole()
    {
        string name = $"Lars {HttpClientExtensions.Unique()} Krämer";

        HttpResponseMessage response = await _admin.PostAsJsonAsync(
            "/api/v1/users",
            new { name },
            HttpClientExtensions.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        UserDto user = (await response.Content.ReadFromJsonAsync<UserDto>(HttpClientExtensions.JsonOptions))!;
        response.Headers.Location!.AbsolutePath.Should().Be($"/api/v1/users/{user.Id}");
        user.Name.Should().Be(name);
        user.Initials.Should().Be("LK");
        user.Color.Should().BeOneOf(_palette);
        user.Role.Should().Be(RoleDto.User);
        user.IsBuiltIn.Should().BeFalse();
        user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Test]
    public async Task CreateUser_WithOwnValues_KeepsThem()
    {
        UserDto user = await _admin.CreateUserAsync(new
        {
            name = $"Own {HttpClientExtensions.Unique()}",
            initials = "Xy",
            color = "#ABCDEF",
            role = "Admin",
        });

        user.Initials.Should().Be("Xy");
        user.Color.Should().Be("#abcdef");
        user.Role.Should().Be(RoleDto.Admin);
    }

    [Test]
    public async Task CreateUser_WithTakenNameInOtherSpelling_ReturnsConflict()
    {
        string unique = HttpClientExtensions.Unique();
        await _admin.CreateUserAsync(new { name = $"Anna {unique}" });

        HttpResponseMessage response = await _admin.PostAsJsonAsync(
            "/api/v1/users",
            new { name = $"  ANNA   {unique.ToUpperInvariant()} " },
            HttpClientExtensions.JsonOptions);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "user.name_taken");
    }

    [TestCase("{\"name\":\"\"}")]
    [TestCase("{\"name\":\"   \"}")]
    [TestCase("{}")]
    [TestCase("{\"name\":\"Valid\",\"color\":\"blue\"}")]
    [TestCase("{\"name\":\"Valid\",\"initials\":\"ABCD\"}")]
    [TestCase("{\"name\":\"Valid\",\"initials\":\"ABCDEFGHIJKLMNOPQ\"}")]
    [TestCase("{\"name\":\"Valid\",\"role\":\"Owner\"}")]
    public async Task CreateUser_WithInvalidInput_ReturnsBadRequest(string body)
    {
        HttpResponseMessage response = await SendAsync(_admin, "POST", "/api/v1/users", body);

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "request.invalid");
    }

    [Test]
    public async Task GetUser_WithId_ReturnsTheUser()
    {
        UserDto created = await _admin.CreateUserAsync(new { name = $"Get {HttpClientExtensions.Unique()}" });

        UserDto user = await _admin.GetJsonAsync<UserDto>($"/api/v1/users/{created.Id}");

        user.Should().BeEquivalentTo(created, options => options.Excluding(dto => dto.CreatedAt));
        user.CreatedAt.Should().BeCloseTo(created.CreatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Test]
    public async Task GetUser_WithUnknownId_ReturnsNotFound()
    {
        HttpResponseMessage response = await _admin.GetAsync($"/api/v1/users/{Guid.NewGuid()}");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "user.not_found");
    }

    [Test]
    public async Task ListUsers_SortedByNameDescending_ReturnsTheUsersInThatOrder()
    {
        string unique = HttpClientExtensions.Unique();
        await _admin.CreateUserAsync(new { name = $"Sort {unique} b" });
        await _admin.CreateUserAsync(new { name = $"Sort {unique} A" });
        await _admin.CreateUserAsync(new { name = $"Sort {unique} c" });

        PageDto<UserDto> page = await _admin.GetJsonAsync<PageDto<UserDto>>(
            "/api/v1/users?sort=Name&direction=Desc&limit=100");

        List<string> names = [.. page.Items.Select(user => user.Name).Where(name => name.Contains(unique))];
        names.Should().Equal($"Sort {unique} c", $"Sort {unique} b", $"Sort {unique} A");
        page.Items.Select(user => user.Name.ToUpperInvariant()).Should().BeInDescendingOrder(StringComparer.Ordinal);
    }

    [Test]
    public async Task ListUsers_SortedByCreation_ReturnsTheOldestFirst()
    {
        PageDto<UserDto> page = await _admin.GetJsonAsync<PageDto<UserDto>>(
            "/api/v1/users?sort=CreatedAt&limit=100");

        page.Items.Select(user => user.CreatedAt).Should().BeInAscendingOrder();
        page.Items[0].IsBuiltIn.Should().BeTrue();
    }

    [Test]
    public async Task ListUsers_FilteredByRole_ReturnsOnlyThatRole()
    {
        await _admin.CreateUserAsync(new { name = $"Filter {HttpClientExtensions.Unique()}", role = "Admin" });

        PageDto<UserDto> page = await _admin.GetJsonAsync<PageDto<UserDto>>("/api/v1/users?role=Admin&limit=100");

        page.Items.Should().NotBeEmpty().And.OnlyContain(user => user.Role == RoleDto.Admin);
        page.Total.Should().Be(page.Items.Count);
    }

    [TestCase("sort=Age")]
    [TestCase("sort=5")]
    [TestCase("direction=Up")]
    [TestCase("role=Owner")]
    [TestCase("limit=101")]
    public async Task ListUsers_WithInvalidQuery_ReturnsBadRequest(string query)
    {
        HttpResponseMessage response = await _admin.GetAsync($"/api/v1/users?{query}");

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "request.invalid");
    }

    [Test]
    public async Task UpdateUser_WithName_LetsDerivedInitialsFollowAndKeepsOwnInitials()
    {
        UserDto derived = await _admin.CreateUserAsync(new { name = $"Anna {HttpClientExtensions.Unique()} Bell" });
        UserDto own = await _admin.CreateUserAsync(new
        {
            name = $"Erik {HttpClientExtensions.Unique()} Fox",
            initials = "QQ",
        });

        UserDto renamed = await PatchAsync(derived.Id, new { name = $"Carla {HttpClientExtensions.Unique()} Diaz" });
        UserDto ownRenamed = await PatchAsync(own.Id, new { name = $"Gina {HttpClientExtensions.Unique()} Hall" });

        renamed.Initials.Should().Be("CD");
        ownRenamed.Initials.Should().Be("QQ");
    }

    [Test]
    public async Task UpdateUser_WithEmptyInitials_DerivesThemFromTheName()
    {
        UserDto user = await _admin.CreateUserAsync(new
        {
            name = $"Ivy {HttpClientExtensions.Unique()} Jones",
            initials = "Z",
        });

        UserDto updated = await PatchAsync(user.Id, new { initials = string.Empty });

        updated.Initials.Should().Be("IJ");
    }

    [Test]
    public async Task UpdateUser_WithoutValues_KeepsEverything()
    {
        UserDto user = await _admin.CreateUserAsync(new { name = $"Keep {HttpClientExtensions.Unique()}" });

        UserDto updated = await PatchAsync(user.Id, new { name = (string?)null });

        updated.Should().BeEquivalentTo(user, options => options.Excluding(dto => dto.CreatedAt));
    }

    [Test]
    public async Task UpdateUser_WithTakenName_ReturnsConflict()
    {
        UserDto first = await _admin.CreateUserAsync(new { name = $"Taken {HttpClientExtensions.Unique()}" });
        UserDto second = await _admin.CreateUserAsync(new { name = $"Other {HttpClientExtensions.Unique()}" });

        HttpResponseMessage response = await _admin.PatchAsJsonAsync(
            $"/api/v1/users/{second.Id}",
            new { name = first.Name.ToLowerInvariant() },
            HttpClientExtensions.JsonOptions);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "user.name_taken");
    }

    [Test]
    public async Task UpdateUser_WithUnknownId_ReturnsNotFound()
    {
        HttpResponseMessage response = await _admin.PatchAsJsonAsync(
            $"/api/v1/users/{Guid.NewGuid()}",
            new { name = "Nobody" },
            HttpClientExtensions.JsonOptions);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "user.not_found");
    }

    [Test]
    public async Task UpdateUser_DemotingTheBuiltInAdmin_ReturnsConflict()
    {
        ProfileDto admin = await _anonymous.GetBuiltInAdminAsync();

        HttpResponseMessage response = await _admin.PatchAsJsonAsync(
            $"/api/v1/users/{admin.Id}",
            new { role = "User" },
            HttpClientExtensions.JsonOptions);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "user.built_in_admin");
    }

    [Test]
    public async Task UpdateUser_RenamingTheBuiltInAdmin_Succeeds()
    {
        ProfileDto admin = await _anonymous.GetBuiltInAdminAsync();

        UserDto renamed = await PatchAsync(admin.Id, new { name = $"Boss{HttpClientExtensions.Unique()}" });
        UserDto restored = await PatchAsync(admin.Id, new { name = "Admin" });

        renamed.Initials.Should().Be("B");
        renamed.IsBuiltIn.Should().BeTrue();
        restored.Name.Should().Be("Admin");
        restored.Initials.Should().Be("A");
    }

    [Test]
    public async Task UpdateUser_WithRole_AppliesToExistingSessionsAtOnce()
    {
        UserDto user = await _admin.CreateUserAsync(new { name = $"Promoted {HttpClientExtensions.Unique()}" });
        using HttpClient client = _factory.CreateAuthorizedClient((await _anonymous.CreateSessionAsync(user.Id)).Token);
        (await client.GetAsync("/api/v1/users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await PatchAsync(user.Id, new { role = "Admin" });
        HttpResponseMessage promoted = await client.GetAsync("/api/v1/users");
        await PatchAsync(user.Id, new { role = "User" });
        HttpResponseMessage demoted = await client.GetAsync("/api/v1/users");

        promoted.StatusCode.Should().Be(HttpStatusCode.OK);
        demoted.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteUser_WithUser_DeletesItAndEndsItsSessions()
    {
        UserDto user = await _admin.CreateUserAsync(new { name = $"Delete {HttpClientExtensions.Unique()}" });
        using HttpClient client = _factory.CreateAuthorizedClient((await _anonymous.CreateSessionAsync(user.Id)).Token);

        HttpResponseMessage deleted = await _admin.DeleteAsync($"/api/v1/users/{user.Id}");
        HttpResponseMessage afterwards = await _admin.GetAsync($"/api/v1/users/{user.Id}");
        HttpResponseMessage session = await client.GetAsync("/api/v1/sessions/current");

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await afterwards.ShouldBeProblemAsync(HttpStatusCode.NotFound, "user.not_found");
        await session.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [Test]
    public async Task DeleteUser_WithBuiltInAdmin_ReturnsConflict()
    {
        ProfileDto admin = await _anonymous.GetBuiltInAdminAsync();

        HttpResponseMessage response = await _admin.DeleteAsync($"/api/v1/users/{admin.Id}");

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "user.built_in_admin");
    }

    [Test]
    public async Task DeleteUser_WithUnknownId_ReturnsNotFound()
    {
        HttpResponseMessage response = await _admin.DeleteAsync($"/api/v1/users/{Guid.NewGuid()}");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "user.not_found");
    }

    private async Task<HttpClient> CreateUserClientAsync()
    {
        UserDto user = await _admin.CreateUserAsync(new { name = $"User {HttpClientExtensions.Unique()}" });
        CreatedSessionDto session = await _anonymous.CreateSessionAsync(user.Id);
        return _factory.CreateAuthorizedClient(session.Token);
    }

    private async Task<UserDto> PatchAsync(Guid id, object body)
    {
        HttpResponseMessage response = await _admin.PatchAsJsonAsync(
            $"/api/v1/users/{id}",
            body,
            HttpClientExtensions.JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<UserDto>(HttpClientExtensions.JsonOptions))!;
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        string method,
        string path,
        string? body)
    {
        using HttpRequestMessage request = new(new HttpMethod(method), path);
        if (body is not null)
        {
            request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        }

        return await client.SendAsync(request);
    }
}
