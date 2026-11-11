using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;

namespace WaveMP.Data.Tests.Migrations;

[TestFixture]
public class ModelSnapshotTests
{
    [Test]
    public void Model_Always_MatchesTheLatestMigration()
    {
        // Fails if an entity configuration is not applied or a model change has no migration yet.
        using WaveMpDbContext dbContext = new WaveMpDbContextFactory().CreateDbContext([]);

        bool hasPendingChanges = dbContext.Database.HasPendingModelChanges();

        hasPendingChanges.Should().BeFalse("every model change needs a migration (dotnet ef migrations add)");
    }
}
