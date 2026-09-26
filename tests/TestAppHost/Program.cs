using Budge.Shared;

namespace Budge.TestAppHost;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        builder
            .AddSqlite(Services.Database);

        builder.Build().Run();
    }
}