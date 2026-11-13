using System.Net;
using System.Text.Json;
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
        HttpResponseMessage response = await _client.GetAsync("/api/does-not-exist", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
