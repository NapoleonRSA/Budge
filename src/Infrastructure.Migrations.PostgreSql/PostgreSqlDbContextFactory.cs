using Budge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Budge.Infrastructure.Migrations.PostgreSql;

public class PostgreSqlDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = "Host=127.0.0.1;Database=budge;Username=design;Password=design";
        var localSettings = FindLocalSettings();
        if (localSettings is not null)
        {
            var configuration = new ConfigurationBuilder().AddJsonFile(localSettings).Build();
            if (DatabaseConfiguration.ResolveProvider(configuration) == "PostgreSQL")
            {
                connectionString = DatabaseConfiguration.ResolveConnectionString(configuration);
            }
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        DatabaseConfiguration.UseProvider(options, "PostgreSQL", connectionString);
        return new ApplicationDbContext(options.Options);
    }

    private static string? FindLocalSettings()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var depth = 0; directory is not null && depth < 6; depth++)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Web", "appsettings.Local.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            candidate = Path.Combine(directory.FullName, "appsettings.Local.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
