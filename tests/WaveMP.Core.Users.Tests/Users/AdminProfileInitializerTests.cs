using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using WaveMP.Core.Users.Users;
using WaveMP.Data.Entities;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Users.Tests.Users;

[TestFixture]
public class AdminProfileInitializerTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 11, 8, 0, 0, TimeSpan.Zero);

    private IUserRepository _users = null!;
    private AdminProfileInitializer _initializer = null!;

    [SetUp]
    public void SetUp()
    {
        _users = Substitute.For<IUserRepository>();
        TimeProvider timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(_now);
        _initializer = new AdminProfileInitializer(_users, timeProvider, NullLogger<AdminProfileInitializer>.Instance);
    }

    [Test]
    public async Task EnsureAsync_WithoutBuiltInAdmin_CreatesIt()
    {
        User? created = null;
        await _users.AddAsync(Arg.Do<User>(user => created = user), Arg.Any<CancellationToken>());

        await _initializer.EnsureAsync();

        created.Should().NotBeNull();
        created!.Name.Should().Be("Admin");
        created.NormalizedName.Should().Be("ADMIN");
        created.Initials.Should().Be("A");
        created.Color.Should().Be("#1f6fd6");
        created.Role.Should().Be(UserRole.Admin);
        created.IsBuiltIn.Should().BeTrue();
        created.CreatedAt.Should().Be(_now);
        created.Settings!.Language.Should().Be(Language.En);
        created.PinHash.Should().BeNull();
    }

    [Test]
    public async Task EnsureAsync_WithBuiltInAdmin_CreatesNothing()
    {
        _users.BuiltInAdminExistsAsync(Arg.Any<CancellationToken>()).Returns(true);

        await _initializer.EnsureAsync();

        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureAsync_WhenAnotherInstanceCreatedItAtTheSameTime_IgnoresTheConflict()
    {
        _users.BuiltInAdminExistsAsync(Arg.Any<CancellationToken>()).Returns(false, true);
        _users.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).ThrowsAsync(new DbUpdateException());

        Func<Task> act = () => _initializer.EnsureAsync();

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task EnsureAsync_WhenSavingFailsForAnotherReason_Throws()
    {
        _users.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).ThrowsAsync(new DbUpdateException());

        Func<Task> act = () => _initializer.EnsureAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
