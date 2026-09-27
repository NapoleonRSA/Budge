using Budge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Budge.Infrastructure.Migrations.MySql;

public class MySqlDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        DatabaseConfiguration.UseProvider(options, "MySql", "server=127.0.0.1;database=budge;user=design;password=design");
        return new ApplicationDbContext(options.Options);
    }
}
