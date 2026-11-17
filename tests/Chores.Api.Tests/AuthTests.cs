using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace Chores.Api.Tests;

public class AuthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Theory]
    [InlineData("/api/session")]
    [InlineData("/api/does-not-exist")]
    public async Task Anonymous_api_requests_are_unauthorized(string path)
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task The_api_key_authenticates_without_a_cookie()
    {
        HttpResponseMessage response = await factory.CreateApiKeyClient().GetAsync("/api/session", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task A_wrong_api_key_is_unauthorized()
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        HttpResponseMessage response = await client.GetAsync("/api/session", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logging_in_with_the_household_password_sets_a_secure_cookie_for_a_year()
    {
        HttpClient client = factory.CreateBrowserClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/login", new { password = ApiFactory.Password }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        string cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        DateTimeOffset expires = DateTimeOffset.Parse(cookie.Split("expires=", StringSplitOptions.None)[1].Split(';')[0]);
        Assert.InRange(expires, DateTimeOffset.UtcNow.AddDays(364), DateTimeOffset.UtcNow.AddDays(366));
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/api/session", TestContext.Current.CancellationToken)).StatusCode);
    }

    [Theory]
    [InlineData("""{ "password": "wrong" }""")]
    [InlineData("{}")]
    public async Task Logging_in_with_a_wrong_password_is_unauthorized(string body)
    {
        HttpClient client = factory.CreateBrowserClient();

        HttpResponseMessage response = await client.PostAsync("/api/login", new StringContent(body, Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/session", TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Logging_out_clears_the_cookie()
    {
        HttpClient client = factory.CreateBrowserClient();
        await client.PostAsJsonAsync("/api/login", new { password = ApiFactory.Password }, TestContext.Current.CancellationToken);

        HttpResponseMessage response = await client.PostAsync("/api/logout", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/session", TestContext.Current.CancellationToken)).StatusCode);
    }
}
