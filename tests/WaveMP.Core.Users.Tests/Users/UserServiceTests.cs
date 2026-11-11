using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using WaveMP.Core.Users.Profiles;
using WaveMP.Core.Users.Users;
using WaveMP.Data.Entities;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Users.Tests.Users;

[TestFixture]
public class UserServiceTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 11, 9, 30, 0, TimeSpan.Zero);

    // A valid name whose first word starts with "á" and 17 more combining marks: one text element of 18 UTF-16 code
    // units, too long for initials.
    private static readonly string _nameWithLongTextElement =
        "\u00E1" + string.Concat(Enumerable.Range(0x0302, 17).Select(mark => (char)mark)) + " x";

    private IUserRepository _users = null!;
    private UserService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _users = Substitute.For<IUserRepository>();
        _users.ListColorsAsync(Arg.Any<CancellationToken>()).Returns([]);
        TimeProvider timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(_now);
        _service = new UserService(_users, timeProvider);
    }

    [Test]
    public async Task CreateAsync_WithNameOnly_DerivesInitialsColorRoleAndLanguage()
    {
        UserResult result = await _service.CreateAsync(new CreateUserCommand("  Lars   Krämer "));

        result.IsSuccess.Should().BeTrue();
        User user = result.User!;
        user.Name.Should().Be("Lars Krämer");
        user.NormalizedName.Should().Be("LARS KRÄMER");
        user.Initials.Should().Be("LK");
        user.Color.Should().Be("#1f6fd6");
        user.Role.Should().Be(UserRole.User);
        user.IsBuiltIn.Should().BeFalse();
        user.CreatedAt.Should().Be(_now);
        user.Settings!.Language.Should().Be(Language.En);
        await _users.Received(1).AddAsync(user, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateAsync_WithUsedColors_PicksLeastUsedColor()
    {
        _users.ListColorsAsync(Arg.Any<CancellationToken>()).Returns(["#1f6fd6"]);

        UserResult result = await _service.CreateAsync(new CreateUserCommand("Anna"));

        result.User!.Color.Should().Be("#0b7a8a");
    }

    [Test]
    public async Task CreateAsync_WithOwnValues_KeepsThem()
    {
        UserResult result = await _service.CreateAsync(
            new CreateUserCommand("Anna", " xy ", "#ABCDEF", UserRole.Admin));

        result.User!.Initials.Should().Be("xy");
        result.User.Color.Should().Be("#abcdef");
        result.User.Role.Should().Be(UserRole.Admin);
        await _users.DidNotReceive().ListColorsAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateAsync_WithEmptyInitials_DerivesThemFromTheName()
    {
        UserResult result = await _service.CreateAsync(new CreateUserCommand("Anna Bell", "  "));

        result.User!.Initials.Should().Be("AB");
    }

    [Test]
    public async Task CreateAsync_WithTooLongTextElementInName_DerivesValidInitials()
    {
        UserResult result = await _service.CreateAsync(new CreateUserCommand(_nameWithLongTextElement));

        result.IsSuccess.Should().BeTrue();
        result.User!.Initials.Should().Be("\u00C1X");
        await _users.Received(1).AddAsync(result.User, Arg.Any<CancellationToken>());
    }

    [TestCase("", null, null, UserError.InvalidName)]
    [TestCase("Anna", "ABCD", null, UserError.InvalidInitials)]
    [TestCase("Anna", null, "blue", UserError.InvalidColor)]
    public async Task CreateAsync_WithInvalidInput_FailsWithoutSaving(
        string name,
        string? initials,
        string? color,
        UserError expected)
    {
        UserResult result = await _service.CreateAsync(new CreateUserCommand(name, initials, color));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(expected);
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateAsync_WithTakenName_FailsWithNameTaken()
    {
        _users.NameExistsAsync("ANNA", null, Arg.Any<CancellationToken>()).Returns(true);

        UserResult result = await _service.CreateAsync(new CreateUserCommand("anna"));

        result.Error.Should().Be(UserError.NameTaken);
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateAsync_WithTakenNameWithCombiningAccent_FailsWithNameTaken()
    {
        // "José" exists; "Jose" with a combining acute accent is the same name.
        _users.NameExistsAsync("JOSÉ", null, Arg.Any<CancellationToken>()).Returns(true);

        UserResult result = await _service.CreateAsync(new CreateUserCommand("José"));

        result.Error.Should().Be(UserError.NameTaken);
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateAsync_WithCombiningAccent_StoresThePrecomposedName()
    {
        UserResult result = await _service.CreateAsync(new CreateUserCommand("José"));

        result.User!.Name.Should().Be("José");
        result.User.NormalizedName.Should().Be("JOSÉ");
        result.User.Initials.Should().Be("J");
    }

    [Test]
    public async Task CreateAsync_WithConflictWhileSaving_FailsWithNameTaken()
    {
        _users.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new UserNameConflictException(new InvalidOperationException()));

        UserResult result = await _service.CreateAsync(new CreateUserCommand("Anna"));

        result.Error.Should().Be(UserError.NameTaken);
    }

    [Test]
    public async Task UpdateAsync_WithUnknownUser_FailsWithNotFound()
    {
        UserResult result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateUserCommand(Name: "Anna"));

        result.Error.Should().Be(UserError.NotFound);
    }

    [Test]
    public async Task UpdateAsync_DemotingTheBuiltInAdmin_FailsWithBuiltInAdmin()
    {
        User admin = Given(CreateUser("Admin", isBuiltIn: true, role: UserRole.Admin));

        UserResult result = await _service.UpdateAsync(admin.Id, new UpdateUserCommand(Role: UserRole.User));

        result.Error.Should().Be(UserError.BuiltInAdmin);
        admin.Role.Should().Be(UserRole.Admin);
        await _users.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_RenamingTheBuiltInAdmin_Succeeds()
    {
        User admin = Given(CreateUser("Admin", isBuiltIn: true, role: UserRole.Admin));

        UserResult result = await _service.UpdateAsync(
            admin.Id,
            new UpdateUserCommand(Name: "Boss", Color: "#2E7D32", Role: UserRole.Admin));

        result.IsSuccess.Should().BeTrue();
        admin.Name.Should().Be("Boss");
        admin.NormalizedName.Should().Be("BOSS");
        admin.Initials.Should().Be("B");
        admin.Color.Should().Be("#2e7d32");
        await _users.Received(1).UpdateAsync(admin, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_WithNewName_LetsDerivedInitialsFollow()
    {
        User user = Given(CreateUser("Anna Bell"));

        await _service.UpdateAsync(user.Id, new UpdateUserCommand(Name: "Carla Diaz"));

        user.Initials.Should().Be("CD");
    }

    [Test]
    public async Task UpdateAsync_WithNewName_KeepsOwnInitials()
    {
        User user = Given(CreateUser("Anna Bell", initials: "XY"));

        await _service.UpdateAsync(user.Id, new UpdateUserCommand(Name: "Carla Diaz"));

        user.Initials.Should().Be("XY");
    }

    [Test]
    public async Task UpdateAsync_WithNewNameAndInitials_UsesTheGivenInitials()
    {
        User user = Given(CreateUser("Anna Bell"));

        await _service.UpdateAsync(user.Id, new UpdateUserCommand(Name: "Carla Diaz", Initials: "Q"));

        user.Initials.Should().Be("Q");
    }

    [Test]
    public async Task UpdateAsync_WithEmptyInitials_DerivesThemFromTheName()
    {
        User user = Given(CreateUser("Anna Bell", initials: "XY"));

        await _service.UpdateAsync(user.Id, new UpdateUserCommand(Initials: string.Empty));

        user.Initials.Should().Be("AB");
    }

    [Test]
    public async Task UpdateAsync_WithNewNameWithTooLongTextElement_LetsDerivedInitialsFollowValidly()
    {
        User user = Given(CreateUser("Anna Bell"));

        UserResult result = await _service.UpdateAsync(user.Id, new UpdateUserCommand(Name: _nameWithLongTextElement));

        result.IsSuccess.Should().BeTrue();
        user.Initials.Should().Be("\u00C1X");
    }

    [Test]
    public async Task UpdateAsync_WithEmptyInitialsAndTooLongTextElementInName_DerivesValidInitials()
    {
        User user = Given(CreateUser(_nameWithLongTextElement, initials: "XY"));

        UserResult result = await _service.UpdateAsync(user.Id, new UpdateUserCommand(Initials: string.Empty));

        result.IsSuccess.Should().BeTrue();
        user.Initials.Should().Be("\u00C1X");
    }

    [Test]
    public async Task UpdateAsync_WithNameOfAnotherUser_FailsWithNameTaken()
    {
        User user = Given(CreateUser("Anna"));
        _users.NameExistsAsync("CARLA", user.Id, Arg.Any<CancellationToken>()).Returns(true);

        UserResult result = await _service.UpdateAsync(user.Id, new UpdateUserCommand(Name: "Carla"));

        result.Error.Should().Be(UserError.NameTaken);
        user.Name.Should().Be("Anna");
    }

    [Test]
    public async Task UpdateAsync_WithNameOfAnotherUserWithCombiningAccent_FailsWithNameTaken()
    {
        User user = Given(CreateUser("Anna"));
        _users.NameExistsAsync("JOSÉ", user.Id, Arg.Any<CancellationToken>()).Returns(true);

        UserResult result = await _service.UpdateAsync(user.Id, new UpdateUserCommand(Name: "josé"));

        result.Error.Should().Be(UserError.NameTaken);
        user.Name.Should().Be("Anna");
    }

    [Test]
    public async Task UpdateAsync_WithConflictWhileSaving_FailsWithNameTaken()
    {
        User user = Given(CreateUser("Anna"));
        _users.UpdateAsync(user, Arg.Any<CancellationToken>())
            .ThrowsAsync(new UserNameConflictException(new InvalidOperationException()));

        UserResult result = await _service.UpdateAsync(user.Id, new UpdateUserCommand(Name: "Carla"));

        result.Error.Should().Be(UserError.NameTaken);
    }

    [TestCase(" ", null, null, UserError.InvalidName)]
    [TestCase(null, "A B", null, UserError.InvalidInitials)]
    [TestCase(null, null, "#12345", UserError.InvalidColor)]
    public async Task UpdateAsync_WithInvalidInput_FailsWithoutSaving(
        string? name,
        string? initials,
        string? color,
        UserError expected)
    {
        User user = Given(CreateUser("Anna"));

        UserResult result = await _service.UpdateAsync(user.Id, new UpdateUserCommand(name, initials, color));

        result.Error.Should().Be(expected);
        await _users.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_WithRole_ChangesTheRole()
    {
        User user = Given(CreateUser("Anna"));

        await _service.UpdateAsync(user.Id, new UpdateUserCommand(Role: UserRole.Admin));

        user.Role.Should().Be(UserRole.Admin);
    }

    [Test]
    public async Task DeleteAsync_WithUnknownUser_ReturnsNotFound()
    {
        UserError? result = await _service.DeleteAsync(Guid.NewGuid());

        result.Should().Be(UserError.NotFound);
    }

    [Test]
    public async Task DeleteAsync_WithBuiltInAdmin_ReturnsBuiltInAdminWithoutDeleting()
    {
        User admin = Given(CreateUser("Admin", isBuiltIn: true, role: UserRole.Admin));

        UserError? result = await _service.DeleteAsync(admin.Id);

        result.Should().Be(UserError.BuiltInAdmin);
        await _users.DidNotReceive().DeleteAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteAsync_WithUser_DeletesIt()
    {
        User user = Given(CreateUser("Anna", role: UserRole.Admin));

        UserError? result = await _service.DeleteAsync(user.Id);

        result.Should().BeNull();
        await _users.Received(1).DeleteAsync(user, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListAsync_Always_PassesTheQueryOn()
    {
        UserListQuery query = new(10, 20, UserSortField.CreatedAt, SortDirection.Desc, UserRole.Admin);
        Page<User> page = new([], 10, 20, 0);
        _users.ListAsync(query, Arg.Any<CancellationToken>()).Returns(page);

        Page<User> result = await _service.ListAsync(query);

        result.Should().BeSameAs(page);
    }

    private User Given(User user)
    {
        _users.FindAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    private static User CreateUser(
        string name,
        string? initials = null,
        bool isBuiltIn = false,
        UserRole role = UserRole.User)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            Initials = initials ?? InitialsGenerator.FromName(name),
            Color = "#1f6fd6",
            Role = role,
            IsBuiltIn = isBuiltIn,
            CreatedAt = _now,
            Settings = new UserSettings(),
        };
    }
}
