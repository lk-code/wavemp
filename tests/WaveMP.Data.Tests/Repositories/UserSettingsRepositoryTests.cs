using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using WaveMP.Data.Entities;
using WaveMP.Data.Repositories;
using WaveMP.Testing.Seeds;

namespace WaveMP.Data.Tests.Repositories;

[TestFixture]
[Category("Integration")]
public class UserSettingsRepositoryTests : LibraryDatabaseTests
{
    protected override string DatabaseName => "wavemp_tests_user_settings";

    [Test]
    public async Task UpdateAsync_PlaybackSettings_AreReadBackUnchanged()
    {
        User user = await AddUserAsync();
        IUserRepository repository = CreateRepository();
        User loaded = (await repository.FindAsync(user.Id))!;
        loaded.Settings = new UserSettings
        {
            UserId = user.Id,
            EqualizerEnabled = true,
            EqualizerPreset = EqualizerPreset.Custom,
            EqualizerPreampDb = -3.5,
            EqualizerCustomGainsDb = [6, 4.5, 3, 1, 0, -1, -2.5, 0, 2, 4],
            CrossfadeSeconds = 7,
            CrossfadeAlbumsGapless = false,
            SleepTimerFadeSeconds = 30,
        };

        await repository.UpdateAsync(loaded);

        UserSettings settings = (await CreateRepository().FindAsync(user.Id))!.Settings!;
        settings.Should().BeEquivalentTo(new
        {
            EqualizerEnabled = true,
            EqualizerPreset = EqualizerPreset.Custom,
            EqualizerPreampDb = -3.5,
            EqualizerCustomGainsDb = new double[] { 6, 4.5, 3, 1, 0, -1, -2.5, 0, 2, 4 },
            CrossfadeSeconds = 7,
            CrossfadeAlbumsGapless = false,
            SleepTimerFadeSeconds = 30,
        });
    }

    [Test]
    public async Task UpdateAsync_ChangedCustomGain_IsSaved()
    {
        User user = await AddUserAsync();
        IUserRepository repository = CreateRepository();
        User loaded = (await repository.FindAsync(user.Id))!;
        loaded.Settings = new UserSettings { UserId = user.Id };
        await repository.UpdateAsync(loaded);

        loaded.Settings.EqualizerCustomGainsDb = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        await repository.UpdateAsync(loaded);

        (await CreateRepository().FindAsync(user.Id))!.Settings!.EqualizerCustomGainsDb
            .Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
    }

    [Test]
    public async Task FindAsync_RowWithoutPlaybackValues_ReadsTheDefaultsOfTheColumns()
    {
        User user = await AddUserAsync();
        await using (WaveMpDbContext dbContext = CreateDbContext())
        {
            // Like a row from before the migration: only the older columns are written.
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO user_settings (user_id, language) VALUES ({user.Id}, 'De')");
        }

        UserSettings settings = (await CreateRepository().FindAsync(user.Id))!.Settings!;

        settings.Should().BeEquivalentTo(new
        {
            Language = Language.De,
            EqualizerEnabled = false,
            EqualizerPreset = EqualizerPreset.Flat,
            EqualizerPreampDb = 0d,
            EqualizerCustomGainsDb = new double[10],
            CrossfadeSeconds = 0,
            CrossfadeAlbumsGapless = true,
            SleepTimerFadeSeconds = 10,
        });
    }

    private UserRepository CreateRepository()
    {
        return new UserRepository(CreateDbContext());
    }

    private async Task<User> AddUserAsync()
    {
        LibrarySeed seed = new();
        User user = seed.User();
        await SaveAsync(seed);
        return user;
    }
}
