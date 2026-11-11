using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.Extensions.DependencyInjection;

namespace WaveMP.Data;

/// <summary>
/// Customizes the <c>dotnet ef</c> tools. Only used at design time, never by the running service.
/// </summary>
internal sealed class WaveMpDesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
    {
        // EF Core picks the last registered C# generator.
        serviceCollection.AddSingleton<IMigrationsCodeGenerator, GeneratedCodeMigrationsGenerator>();
    }
}
