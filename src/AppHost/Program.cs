using Budge.Shared;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("aca-env");

var configuredProvider = Environment.GetEnvironmentVariable("Database__Provider");
var useExternalDatabase = !string.IsNullOrWhiteSpace(configuredProvider)
    && !configuredProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase);

var web = builder.AddProject<Projects.Web>(Services.WebApi)
    .WithExternalHttpEndpoints()
    .WithAspNetCoreEnvironment()
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Scalar API Reference";
        url.Url = "/scalar";
    });

if (!useExternalDatabase)
{
    var databaseServer = builder.AddSqlite(Services.Database);
    web = web
        .WithReference(databaseServer)
        .WaitFor(databaseServer)
        .WithEnvironment("Database__Provider", "Sqlite");
}

var developmentAdminPassword = builder.Configuration["Development:AdminPassword"];
if (!string.IsNullOrWhiteSpace(developmentAdminPassword))
{
    web = web.WithEnvironment("Development__AdminPassword", developmentAdminPassword);
}

if (builder.ExecutionContext.IsRunMode)
{
    builder.AddJavaScriptApp(Services.WebFrontend, "./../Web/ClientApp")
        .WithRunScript("start")
        .WithReference(web)
        .WaitFor(web)
        .WithHttpEndpoint(env: "PORT")
        .WithExternalHttpEndpoints();
}

builder.Build().Run();
