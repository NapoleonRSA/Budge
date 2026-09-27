using Budge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Budge.Infrastructure.Migrations.SqlServer;

public class SqlServerDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        DatabaseConfiguration.UseProvider(options, "SqlServer", "Server=127.0.0.1;Database=budge;User Id=design;Password=design;TrustServerCertificate=True");
        return new ApplicationDbContext(options.Options);
    }
}
