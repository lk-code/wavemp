using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Users.Settings;
using WaveMP.Data.Entities;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Users.Tests.Settings;

[TestFixture]
public class UserSettingsServiceTests
{
    private IUserRepository _users = null!;
    private UserSettingsService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _users = Substitute.For<IUserRepository>();
        _service = new UserSettingsService(_users);
    }

    [Test]
    public async Task GetAsync_WithUser_ReturnsItsSettings()
    {
        User user = Given(Language.De);

        UserSettings? result = await _service.GetAsync(user.Id);

        result.Should().BeSameAs(user.Settings);
    }

    [Test]
    public async Task GetAsync_WithUnknownUser_ReturnsNull()
    {
        UserSettings? result = await _service.GetAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Test]
    public async Task UpdateAsync_WithLanguage_SavesIt()
    {
        User user = Given(Language.En);

        UserSettings? result = await _service.UpdateAsync(user.Id, new UpdateUserSettingsCommand(Language.De));

        result!.Language.Should().Be(Language.De);
        await _users.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_WithoutLanguage_KeepsIt()
    {
        User user = Given(Language.De);

        UserSettings? result = await _service.UpdateAsync(user.Id, new UpdateUserSettingsCommand());

        result!.Language.Should().Be(Language.De);
    }

    [Test]
    public async Task UpdateAsync_WithUnknownUser_ReturnsNull()
    {
        UserSettings? result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateUserSettingsCommand(Language.De));

        result.Should().BeNull();
        await _users.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    private User Given(Language language)
    {
        User user = new()
        {
            Id = Guid.NewGuid(),
            Name = "Anna",
            NormalizedName = "ANNA",
            Initials = "A",
            Color = "#1f6fd6",
            Role = UserRole.User,
            IsBuiltIn = false,
            CreatedAt = DateTimeOffset.UnixEpoch,
            Settings = new UserSettings { Language = language },
        };
        _users.FindAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }
}
