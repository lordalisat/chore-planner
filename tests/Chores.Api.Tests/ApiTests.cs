using System.Net;
using System.Text.Json;
using Chores.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Chores.Api.Tests;

public class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_endpoint_reports_ok()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_document_describes_the_api()
    {
        using var document = JsonDocument.Parse(
            await _client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken));

        Assert.True(document.RootElement.GetProperty("paths").TryGetProperty("/api/health", out _));
    }

    [Fact]
    public async Task Requests_forwarded_by_the_reverse_proxy_see_its_public_https_address()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/openapi/v1.json");
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Add("X-Forwarded-Host", "chores.example");
        request.Headers.Add("X-Forwarded-For", "192.168.1.20");

        HttpResponseMessage response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.Equal("https://chores.example/", document.RootElement.GetProperty("servers")[0].GetProperty("url").GetString());
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/chores/42")]
    public async Task Client_routes_serve_the_front_end(string path)
    {
        string html = await _client.GetStringAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(ApiFactory.IndexHtml, html);
    }

    [Fact]
    public async Task Unknown_api_routes_are_not_found()
    {
        HttpResponseMessage response = await factory.CreateApiKeyClient().GetAsync("/api/does-not-exist", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void Startup_migrates_the_configured_database()
    {
        using IServiceScope scope = factory.Services.CreateScope();
        ChoresDbContext db = scope.ServiceProvider.GetRequiredService<ChoresDbContext>();

        Assert.True(File.Exists(factory.DatabasePath));
        Assert.Empty(db.Database.GetPendingMigrations());
    }
}
