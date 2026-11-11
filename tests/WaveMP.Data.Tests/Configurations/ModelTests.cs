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
