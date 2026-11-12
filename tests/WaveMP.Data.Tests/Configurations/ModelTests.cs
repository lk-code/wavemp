using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Tests.Configurations;

[TestFixture]
public class ModelTests
{
    private WaveMpDbContext _dbContext = null!;
    private IModel _model = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _dbContext = new WaveMpDbContextFactory().CreateDbContext([]);
        // The design-time model keeps everything migrations use, e.g. index filters.
        _model = _dbContext.GetService<IDesignTimeModel>().Model;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _dbContext.Dispose();
    }

    [TestCase(typeof(User), "users")]
    [TestCase(typeof(UserSettings), "user_settings")]
    [TestCase(typeof(Session), "sessions")]
    [TestCase(typeof(ServerSecret), "server_secrets")]
    [TestCase(typeof(ServerSetting), "server_settings")]
    [TestCase(typeof(JobRun), "job_runs")]
    [TestCase(typeof(Artist), "artists")]
    [TestCase(typeof(Album), "albums")]
    [TestCase(typeof(Track), "tracks")]
    [TestCase(typeof(MediaFile), "media_files")]
    [TestCase(typeof(Cover), "covers")]
    [TestCase(typeof(TagBackup), "tag_backups")]
    [TestCase(typeof(PlayHistoryEntry), "play_history")]
    [TestCase(typeof(Favorite), "favorites")]
    [TestCase(typeof(TrackRating), "track_ratings")]
    public void Entity_Always_HasSnakeCaseTable(Type entityType, string expectedTable)
    {
        IEntityType entity = GetEntity(entityType);

        entity.GetTableName().Should().Be(expectedTable);
    }

    [TestCase(nameof(User.NormalizedName), "normalized_name")]
    [TestCase(nameof(User.IsBuiltIn), "is_built_in")]
    [TestCase(nameof(User.PinHash), "pin_hash")]
    [TestCase(nameof(User.CreatedAt), "created_at")]
    public void User_Always_HasSnakeCaseColumns(string property, string expectedColumn)
    {
        IProperty column = GetEntity(typeof(User)).FindProperty(property)!;

        column.GetColumnName().Should().Be(expectedColumn);
    }

    [Test]
    public void User_NormalizedName_HasUniqueIndex()
    {
        IIndex index = FindIndex(nameof(User.NormalizedName));

        index.IsUnique.Should().BeTrue();
        index.GetDatabaseName().Should().Be("ix_users_normalized_name");
    }

    [Test]
    public void User_IsBuiltIn_HasFilteredUniqueIndexForASingleBuiltInAdmin()
    {
        IIndex index = FindIndex(nameof(User.IsBuiltIn));

        index.IsUnique.Should().BeTrue();
        index.GetFilter().Should().Be("is_built_in");
    }

    [Test]
    public void User_PinHash_IsOptional()
    {
        IProperty pinHash = GetEntity(typeof(User)).FindProperty(nameof(User.PinHash))!;

        pinHash.IsNullable.Should().BeTrue();
    }

    [TestCase(typeof(Session))]
    [TestCase(typeof(UserSettings))]
    [TestCase(typeof(PlayHistoryEntry))]
    public void DependentsOfUser_Always_AreDeletedWithTheUser(Type dependentType)
    {
        IForeignKey foreignKey = GetEntity(dependentType).GetForeignKeys()
            .Single(key => key.PrincipalEntityType.ClrType == typeof(User));

        foreignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }

    [Test]
    public void Role_Always_IsStoredAsName()
    {
        IProperty role = GetEntity(typeof(User)).FindProperty(nameof(User.Role))!;

        role.GetProviderClrType().Should().Be<string>();
    }

    [Test]
    public void UserSettings_ReplayGain_IsStoredAsNameWithOffAsDefault()
    {
        IProperty replayGain = GetEntity(typeof(UserSettings)).FindProperty(nameof(UserSettings.ReplayGain))!;

        replayGain.GetProviderClrType().Should().Be<string>();
        replayGain.GetDefaultValue().Should().Be(ReplayGainMode.Off);
    }

    [Test]
    public void ServerSetting_Value_IsStoredAsJsonb()
    {
        IProperty value = GetEntity(typeof(ServerSetting)).FindProperty(nameof(ServerSetting.Value))!;

        value.GetColumnType().Should().Be("jsonb");
    }

    [TestCase(nameof(JobRun.Trigger))]
    [TestCase(nameof(JobRun.Status))]
    [TestCase(nameof(JobRun.ErrorCode))]
    public void JobRun_Enums_AreStoredAsNames(string property)
    {
        IProperty column = GetEntity(typeof(JobRun)).FindProperty(property)!;

        column.GetProviderClrType().Should().Be<string>();
    }

    [Test]
    public void JobRun_Result_IsStoredAsJsonb()
    {
        IProperty result = GetEntity(typeof(JobRun)).FindProperty(nameof(JobRun.Result))!;

        result.GetColumnType().Should().Be("jsonb");
        result.IsNullable.Should().BeTrue();
    }

    [TestCase(typeof(MediaFile), new[] { nameof(MediaFile.RelativePath) })]
    [TestCase(typeof(Track), new[] { nameof(Track.DirectoryPath), nameof(Track.NormalizedBaseName) })]
    [TestCase(typeof(Album), new[] { nameof(Album.AlbumKey) })]
    [TestCase(typeof(Artist), new[] { nameof(Artist.NormalizedName) })]
    public void Library_NaturalKeys_HaveUniqueIndexes(Type entityType, string[] properties)
    {
        IIndex index = GetEntity(entityType).GetIndexes()
            .Single(candidate => candidate.Properties.Select(property => property.Name).SequenceEqual(properties));

        index.IsUnique.Should().BeTrue();
    }

    [Test]
    public void Cover_Always_BelongsToEitherAnAlbumOrATrack()
    {
        ICheckConstraint constraint = GetEntity(typeof(Cover)).GetCheckConstraints().Single();

        constraint.Name.Should().Be("ck_covers_owner");
        constraint.Sql.Should().Be("(album_id IS NULL) <> (track_id IS NULL)");
    }

    [TestCase(typeof(Cover), typeof(Album), DeleteBehavior.Cascade)]
    [TestCase(typeof(Cover), typeof(Track), DeleteBehavior.Cascade)]
    [TestCase(typeof(Cover), typeof(MediaFile), DeleteBehavior.Cascade)]
    [TestCase(typeof(Track), typeof(Album), DeleteBehavior.SetNull)]
    [TestCase(typeof(Track), typeof(Artist), DeleteBehavior.SetNull)]
    [TestCase(typeof(Album), typeof(Artist), DeleteBehavior.SetNull)]
    [TestCase(typeof(MediaFile), typeof(Track), DeleteBehavior.SetNull)]
    [TestCase(typeof(TagBackup), typeof(MediaFile), DeleteBehavior.SetNull)]
    [TestCase(typeof(PlayHistoryEntry), typeof(Track), DeleteBehavior.Cascade)]
    public void Library_ForeignKeys_HaveTheirDeleteBehavior(Type dependent, Type principal, DeleteBehavior expected)
    {
        IForeignKey foreignKey = GetEntity(dependent).GetForeignKeys()
            .Single(key => key.PrincipalEntityType.ClrType == principal);

        foreignKey.DeleteBehavior.Should().Be(expected);
    }

    [TestCase(typeof(MediaFile), nameof(MediaFile.Kind))]
    [TestCase(typeof(MediaFile), nameof(MediaFile.Issue))]
    [TestCase(typeof(Cover), nameof(Cover.Source))]
    [TestCase(typeof(Track), nameof(Track.MetadataSource))]
    public void Library_Enums_AreStoredAsNames(Type entityType, string property)
    {
        IProperty column = GetEntity(entityType).FindProperty(property)!;

        column.GetProviderClrType().Should().Be<string>();
    }

    [Test]
    public void Model_Always_HasTheTrigramExtensionOfTheSearch()
    {
        IEnumerable<string> extensions = _model.GetPostgresExtensions().Select(extension => extension.Name);

        extensions.Should().Contain("pg_trgm");
    }

    [TestCase(typeof(Artist), nameof(Artist.Name), "ix_artists_name_trgm")]
    [TestCase(typeof(Album), nameof(Album.Title), "ix_albums_title_trgm")]
    [TestCase(typeof(Track), nameof(Track.Title), "ix_tracks_title_trgm")]
    public void Library_SearchedTexts_HaveTrigramIndexes(Type entityType, string property, string expectedName)
    {
        IIndex index = GetEntity(entityType).GetIndexes()
            .Single(candidate => candidate.Properties.Count == 1 && candidate.Properties[0].Name == property);

        index.GetDatabaseName().Should().Be(expectedName);
        index.GetMethod().Should().Be("gin");
        index.GetOperators().Should().Equal("gin_trgm_ops");
        index.IsUnique.Should().BeFalse();
    }

    [Test]
    public void MediaFile_Tags_AreColumnsOfTheFileTable()
    {
        IComplexProperty tags = GetEntity(typeof(MediaFile)).FindComplexProperty(nameof(MediaFile.Tags))!;

        tags.ComplexType.FindProperty(nameof(MediaFileTags.Title))!.GetColumnName().Should().Be("tags_title");
        tags.ComplexType.FindProperty(nameof(MediaFileTags.ReplayGainTrackGain))!.GetColumnName()
            .Should().Be("tags_replay_gain_track_gain");
    }

    [TestCase(nameof(TagBackup.Performers))]
    [TestCase(nameof(TagBackup.AlbumArtists))]
    [TestCase(nameof(TagBackup.Genres))]
    public void TagBackup_Lists_AreTextArrays(string property)
    {
        IProperty column = GetEntity(typeof(TagBackup)).FindProperty(property)!;

        column.GetColumnType().Should().Be("text[]");
        column.IsNullable.Should().BeFalse();
    }

    [Test]
    public void TagBackup_Texts_HaveNoLengthLimit()
    {
        IProperty title = GetEntity(typeof(TagBackup)).FindProperty(nameof(TagBackup.Title))!;

        title.GetColumnType().Should().Be("text");
    }

    private IEntityType GetEntity(Type type)
    {
        return _model.FindEntityType(type) ?? throw new InvalidOperationException($"{type.Name} is not mapped.");
    }

    private IIndex FindIndex(string property)
    {
        return GetEntity(typeof(User)).GetIndexes()
            .Single(index => index.Properties.Count == 1 && index.Properties[0].Name == property);
    }
}
