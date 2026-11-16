using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace API.Tests;

// Runs the API with a test-only signing key and a throwaway SQLite file, so tests never use user-secrets or app.db.
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "test-only-signing-key-that-is-at-least-32-bytes";
    public const string Issuer = "JwtAuthentication.Issuer";
    public const string Audience = "JwtAuthentication.Api";

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"jwt-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development runs the startup migration against the temp database. In-memory settings below override user-secrets.
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = SigningKey,
            ["ConnectionStrings:Default"] = $"Data Source={_databasePath}"
        }));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // SQLite keeps the file open through pooled connections, so release them before deleting the file.
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}
