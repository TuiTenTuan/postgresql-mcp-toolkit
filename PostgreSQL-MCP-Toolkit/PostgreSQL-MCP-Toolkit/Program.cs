using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var timeout = EnvConfigUti.Instance.GetEnvironmentConfig("TIMEOUT");
if (string.IsNullOrWhiteSpace(timeout))
{
    timeout = "1024";
}

var postgresHost = EnvConfigUti.Instance.GetRequiredEnvironmentConfig("POSTGRES_HOST");
var postgresPort = EnvConfigUti.Instance.GetEnvironmentConfig("POSTGRES_PORT");
if (string.IsNullOrWhiteSpace(postgresPort))
{
    postgresPort = "5432";
}

var postgresUser = EnvConfigUti.Instance.GetRequiredEnvironmentConfig("POSTGRES_USER");
var postgresPassword = EnvConfigUti.Instance.GetRequiredEnvironmentConfig("POSTGRES_PASSWORD");
var postgresDatabase = EnvConfigUti.Instance.GetRequiredEnvironmentConfig("POSTGRES_DATABASE");
var apiKey = EnvConfigUti.Instance.GetRequiredEnvironmentConfig("API_KEY");


var connectionString = new NpgsqlConnectionStringBuilder
{
    Host = postgresHost,
    Port = int.Parse(postgresPort),
    Username = postgresUser,
    Password = postgresPassword,
    Database = postgresDatabase,
    Timeout = int.Parse(timeout),
    CommandTimeout = int.Parse(timeout),
};

builder.Services.AddSingleton(new ApiKeyOptions(apiKey));
builder.Services.AddSingleton(new PostgreSQLDB(connectionString, int.Parse(timeout)));

builder.Services.AddCors(options =>
{
    options.AddPolicy("MCPPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddHealthChecks();

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .WithTools<PostgreSQLTools>();

var app = builder.Build();

app.UseCors("MCPPolicy");
app.UseRouting();

app.Use(async (context, next) =>
{
    if (HttpMethods.IsOptions(context.Request.Method) || context.Request.Path.StartsWithSegments("/health"))
    {
        await next();
        return;
    }

    var apiKeyOptions = context.RequestServices.GetRequiredService<ApiKeyOptions>();
    if (!context.Request.Headers.TryGetValue("x-api-key", out var providedKeyValues) ||
        providedKeyValues.Count == 0 ||
        string.IsNullOrEmpty(providedKeyValues[0]) ||
        !APIKeyUti.Instance.Equals(providedKeyValues[0]!, apiKeyOptions.ApiKey))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    await next();
});

app.MapHealthChecks("/health");
app.MapMcp();

app.Run();

internal sealed record ApiKeyOptions(string ApiKey);
