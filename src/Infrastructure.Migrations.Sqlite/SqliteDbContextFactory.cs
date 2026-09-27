using Budge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Budge.Infrastructure.Migrations.Sqlite;

public class SqliteDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        DatabaseConfiguration.UseProvider(options, "Sqlite", "Data Source=design.db");
        return new ApplicationDbContext(options.Options);
    }
}
