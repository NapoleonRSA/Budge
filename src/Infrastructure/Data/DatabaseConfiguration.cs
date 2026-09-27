using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Npgsql;

namespace Budge.Infrastructure.Data;

public static class DatabaseConfiguration
{
    public const string ProviderKey = "Database:Provider";

    public static string Normalize(string? provider)
    {
        switch (provider?.Trim().ToLowerInvariant())
        {
            case null:
            case "":
            case "sqlite":
                return "Sqlite";
            case "mysql":
            case "mariadb":
                return "MySql";
            case "postgres":
            case "postgresql":
            case "npgsql":
                return "PostgreSQL";
            case "sqlserver":
            case "mssql":
                return "SqlServer";
            default:
                throw new InvalidOperationException(
                    $"Database provider '{provider}' is not supported. Use Sqlite, MySql, PostgreSQL, or SqlServer.");
        }
    }

    public static string MigrationsAssemblyName(string provider) => Normalize(provider) switch
    {
        "MySql" => "Budge.Infrastructure.Migrations.MySql",
        "PostgreSQL" => "Budge.Infrastructure.Migrations.PostgreSql",
        "SqlServer" => "Budge.Infrastructure.Migrations.SqlServer",
        _ => "Budge.Infrastructure.Migrations.Sqlite"
    };

    public static string ResolveProvider(IConfiguration configuration) =>
        Normalize(configuration[ProviderKey]);

    public static string ResolveConnectionString(IConfiguration configuration)
    {
        var provider = ResolveProvider(configuration);
        if (provider == "Sqlite")
        {
            return RequireConfiguredConnectionString(configuration);
        }

        var host = configuration["Database:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            return RequireConfiguredConnectionString(configuration);
        }

        var username = configuration["Database:Username"];
        var password = configuration["Database:Password"];
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Database username and password are missing. Enter them in src/Web/appsettings.Local.json.");
        }

        var name = configuration["Database:Name"];
        var database = string.IsNullOrWhiteSpace(name) ? "budge" : name.Trim();
        var port = ResolvePort(configuration["Database:Port"], provider);

        return provider switch
        {
            "PostgreSQL" => new NpgsqlConnectionStringBuilder
            {
                Host = host.Trim(),
                Port = port,
                Database = database,
                Username = username,
                Password = password
            }.ConnectionString,
            "MySql" => new MySqlConnectionStringBuilder
            {
                Server = host.Trim(),
                Port = (uint)port,
                Database = database,
                UserID = username,
                Password = password
            }.ConnectionString,
            "SqlServer" => new SqlConnectionStringBuilder
            {
                DataSource = port == 1433 ? host.Trim() : $"{host.Trim()},{port}",
                InitialCatalog = database,
                UserID = username,
                Password = password,
                TrustServerCertificate = true
            }.ConnectionString,
            _ => RequireConfiguredConnectionString(configuration)
        };
    }

    private static string RequireConfiguredConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(Services.Database);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{Services.Database}' not found.");
        }

        return connectionString;
    }

    private static int ResolvePort(string? port, string provider)
    {
        if (int.TryParse(port, out var parsed) && parsed is > 0 and <= 65535)
        {
            return parsed;
        }

        return provider switch
        {
            "PostgreSQL" => 5432,
            "MySql" => 3306,
            "SqlServer" => 1433,
            _ => 0
        };
    }

    public static void UseProvider(DbContextOptionsBuilder options, string provider, string connectionString)
    {
        var normalized = Normalize(provider);
        var assembly = MigrationsAssemblyName(normalized);

        if (normalized != "Sqlite" && connectionString.Contains("DataSource=", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Database:Provider is not Sqlite, but ConnectionStrings:BudgeDb is still a SQLite connection string. Set ConnectionStrings__BudgeDb in the environment.");
        }

        switch (normalized)
        {
            case "MySql":
                options.UseMySQL(connectionString, mysql => mysql.MigrationsAssembly(assembly));
                break;
            case "PostgreSQL":
                options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(assembly));
                break;
            case "SqlServer":
                options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(assembly));
                break;
            default:
                options.UseSqlite(connectionString, sqlite => sqlite.MigrationsAssembly(assembly));
                break;
        }
    }
}
