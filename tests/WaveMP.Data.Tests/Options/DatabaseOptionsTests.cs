using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NSubstitute;
using WaveMP.Data.Extensions;
using WaveMP.Data.Options;

namespace WaveMP.Data.Tests.Options;

[TestFixture]
public class DatabaseOptionsTests
{
    [Test]
    public void BuildConnectionString_WithDefaults_UsesDefaultValues()
    {
        DatabaseOptions options = new() { Password = "secret" };

        NpgsqlConnectionStringBuilder result = new(options.BuildConnectionString());

        result.Host.Should().Be("localhost");
        result.Port.Should().Be(5432);
        result.Database.Should().Be("wavemp");
        result.Username.Should().Be("wavemp");
        result.Password.Should().Be("secret");
    }

    [Test]
    public void BindDatabaseOptions_WithEnvironmentValues_OverridesDefaults()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["DB_HOST"] = "wavemp-db",
            ["DB_PORT"] = "5433",
            ["DB_NAME"] = "music",
            ["DB_USER"] = "player",
            ["DB_PASSWORD"] = "p@ss;word",
        });
        DatabaseOptions options = new();

        ServiceCollectionExtensions.BindDatabaseOptions(options, configuration);

        options.Host.Should().Be("wavemp-db");
        options.Port.Should().Be(5433);
        options.Name.Should().Be("music");
        options.User.Should().Be("player");
        options.Password.Should().Be("p@ss;word");
        new NpgsqlConnectionStringBuilder(options.BuildConnectionString()).Password.Should().Be("p@ss;word");
    }

    [Test]
    public void BindDatabaseOptions_WithoutValues_KeepsDefaults()
    {
        DatabaseOptions options = new();

        ServiceCollectionExtensions.BindDatabaseOptions(options, CreateConfiguration([]));

        options.Host.Should().Be(DatabaseOptions.DefaultHost);
        options.Port.Should().Be(DatabaseOptions.DefaultPort);
        options.Password.Should().BeEmpty();
    }

    [Test]
    public void Validate_WithoutPassword_FailsWithHint()
    {
        DatabaseOptions options = new();

        List<ValidationResult> results = Validate(options);

        results.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("DB_PASSWORD");
    }

    [Test]
    public void Validate_WithInvalidPort_Fails()
    {
        DatabaseOptions options = new() { Password = "secret" };
        ServiceCollectionExtensions.BindDatabaseOptions(
            options,
            CreateConfiguration(new Dictionary<string, string?> { ["DB_PORT"] = "not-a-port" }));

        List<ValidationResult> results = Validate(options);

        results.Should().ContainSingle()
            .Which.MemberNames.Should().Contain(nameof(DatabaseOptions.Port));
    }

    [Test]
    public void Validate_WithPassword_Succeeds()
    {
        DatabaseOptions options = new() { Password = "secret" };

        List<ValidationResult> results = Validate(options);

        results.Should().BeEmpty();
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        IConfiguration configuration = Substitute.For<IConfiguration>();
        configuration[Arg.Any<string>()].Returns(call => values.GetValueOrDefault(call.Arg<string>()));
        return configuration;
    }

    private static List<ValidationResult> Validate(DatabaseOptions options)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
