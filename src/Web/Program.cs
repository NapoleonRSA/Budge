using Budge.Infrastructure.Data;
using Budge.Web;
using Scalar.AspNetCore;

LocalEnvironment.Load();

var builder = WebApplication.CreateBuilder(args);

if (!string.Equals(Environment.GetEnvironmentVariable("BUDGE_LOAD_LOCAL_CONFIG"), "0", StringComparison.Ordinal))
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
    builder.Configuration.AddEnvironmentVariables();
}

// Add services to the container.
builder.AddServiceDefaults();

builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors(static builder => 
    builder.AllowAnyMethod()
        .AllowAnyHeader()
        .AllowAnyOrigin());

app.UseFileServer();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseExceptionHandler(options => { });


app.MapDefaultEndpoints();
app.MapEndpoints(typeof(Program).Assembly);

app.MapFallbackToFile("index.html");

app.Run();
