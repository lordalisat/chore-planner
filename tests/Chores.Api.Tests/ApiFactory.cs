using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Chores.Api.Tests;

/// <summary>Runs the API against a throwaway web root standing in for the built front end, and a throwaway database.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string ApiKey = "test-api-key";
    public const string Password = "correct horse";
    // PasswordHasher v3 format: PBKDF2-HMAC-SHA512, 100,000 iterations, salt 00..0f
    private const string PasswordHash = "AQAAAAIAAYagAAAAEAABAgMEBQYHCAkKCwwNDg8LJ1+nXP1HCEVtkGVQ8YRRub3RhgjMuQMQ6K7+XxmQVQ==";
    public const string IndexHtml = "<!doctype html><title>Chores test page</title>";
    /// <summary>Today for the API under test: a Wednesday.</summary>
    public static readonly DateOnly Today = new(2027, 3, 10);

    private readonly DirectoryInfo _webRoot = Directory.CreateTempSubdirectory("chores-wwwroot-");

    public string DatabasePath { get; } = Path.Combine(Directory.CreateTempSubdirectory("chores-data-").FullName, "chores.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        File.WriteAllText(Path.Combine(_webRoot.FullName, "index.html"), IndexHtml);
        builder.UseWebRoot(_webRoot.FullName);
        builder.UseSetting("Database:Path", DatabasePath);
        builder.UseSetting("Auth:ApiKey", ApiKey);
        builder.UseSetting("Auth:PasswordHash", PasswordHash);
        // Development would layer the project's own wwwroot (a built front end, if any) over this web root
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(new FixedClock(Today)));
    }

    /// <summary>A client authenticated the way AI tooling is, with the API key header.</summary>
    public HttpClient CreateApiKeyClient()
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        return client;
    }

    /// <summary>A client that keeps cookies over HTTPS, the way the PWA does, so the secure cookie is sent back.</summary>
    public HttpClient CreateBrowserClient() => CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        _webRoot.Delete(recursive: true);
        Directory.Delete(Path.GetDirectoryName(DatabasePath)!, recursive: true);
    }
}
