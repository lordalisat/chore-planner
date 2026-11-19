using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Chores.Api.Tests;

/// <summary>Deleting a Chore: Archived when it has Completions, removed otherwise; against a clock fixed on 10 March 2027.</summary>
public class DeleteChoreTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateApiKeyClient();

    [Fact]
    public async Task A_Chore_without_Completions_is_removed()
    {
        int id = await Create("Clean the oven");

        HttpResponseMessage response = await _client.DeleteAsync($"/api/chores/{id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement result = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.False(result.GetProperty("archived").GetBoolean());
        Assert.DoesNotContain(await Chores("/api/chores"), c => c.GetProperty("id").GetInt32() == id);
        Assert.DoesNotContain(await Chores("/api/chores?archived=true"), c => c.GetProperty("id").GetInt32() == id);
        Assert.Equal(HttpStatusCode.NotFound, (await Restore(id)).StatusCode);
    }

    [Fact]
    public async Task A_Chore_with_Completions_is_Archived_with_its_history()
    {
        int id = await Create("Laundry", lastDone: "2027-03-04");

        HttpResponseMessage response = await _client.DeleteAsync($"/api/chores/{id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement result = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.True(result.GetProperty("archived").GetBoolean());
        Assert.DoesNotContain(await Chores("/api/chores"), c => c.GetProperty("id").GetInt32() == id);
        JsonElement archived = Assert.Single(await Chores("/api/chores?archived=true"), c => c.GetProperty("id").GetInt32() == id);
        Assert.Equal("2027-03-10", archived.GetProperty("archivedOn").GetString());
        Assert.Equal("2027-03-04", archived.GetProperty("lastDone").GetString());
        Assert.Equal(1, archived.GetProperty("completionCount").GetInt32());
        Assert.False(archived.GetProperty("overdue").GetBoolean());
        Assert.Equal(JsonValueKind.Null, archived.GetProperty("urgency").ValueKind);
    }

    [Fact]
    public async Task Archived_Chores_are_only_listed_when_asked_for()
    {
        int active = await Create("Water the plants");
        int archived = await Create("Defrost the freezer", lastDone: "2027-01-10");
        await _client.DeleteAsync($"/api/chores/{archived}", TestContext.Current.CancellationToken);

        List<JsonElement> listed = await Chores("/api/chores");
        List<JsonElement> listedArchived = await Chores("/api/chores?archived=true");

        Assert.Contains(listed, c => c.GetProperty("id").GetInt32() == active);
        Assert.All(listed, c => Assert.Equal(JsonValueKind.Null, c.GetProperty("archivedOn").ValueKind));
        Assert.DoesNotContain(listedArchived, c => c.GetProperty("id").GetInt32() == active);
        Assert.All(listedArchived, c => Assert.NotEqual(JsonValueKind.Null, c.GetProperty("archivedOn").ValueKind));
    }

    [Fact]
    public async Task Restore_brings_an_Archived_Chore_back_with_its_history()
    {
        int id = await Create("Change the bed", lastDone: "2027-03-04");
        await _client.DeleteAsync($"/api/chores/{id}", TestContext.Current.CancellationToken);

        HttpResponseMessage response = await Restore(id);

        Assert.True(response.IsSuccessStatusCode);
        JsonElement restored = Assert.Single(await Chores("/api/chores"), c => c.GetProperty("id").GetInt32() == id);
        Assert.Equal("2027-03-04", restored.GetProperty("lastDone").GetString());
        Assert.Equal(JsonValueKind.Null, restored.GetProperty("archivedOn").ValueKind);
        Assert.DoesNotContain(await Chores("/api/chores?archived=true"), c => c.GetProperty("id").GetInt32() == id);
    }

    [Fact]
    public async Task Only_an_Archived_Chore_can_be_Restored()
    {
        int id = await Create("Descale the kettle", lastDone: "2027-03-01");

        Assert.Equal(HttpStatusCode.NotFound, (await Restore(id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Restore(999_999)).StatusCode);
    }

    [Fact]
    public async Task An_Archived_Chore_cannot_be_deleted_for_good()
    {
        int id = await Create("Clean the gutters", lastDone: "2027-03-01");
        await _client.DeleteAsync($"/api/chores/{id}", TestContext.Current.CancellationToken);

        HttpResponseMessage response = await _client.DeleteAsync($"/api/chores/{id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Single(await Chores("/api/chores?archived=true"), c => c.GetProperty("id").GetInt32() == id);
    }

    [Fact]
    public async Task An_Archived_Chore_cannot_be_read_or_edited()
    {
        int id = await Create("Sweep the chimney", lastDone: "2027-03-01");
        await _client.DeleteAsync($"/api/chores/{id}", TestContext.Current.CancellationToken);

        HttpResponseMessage read = await _client.GetAsync($"/api/chores/{id}", TestContext.Current.CancellationToken);
        HttpResponseMessage edit = await _client.PutAsJsonAsync(
            $"/api/chores/{id}", new { name = "Sweep the chimney", interval = new { count = 2, unit = "Week" } },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, edit.StatusCode);
        JsonElement archived = Assert.Single(await Chores("/api/chores?archived=true"), c => c.GetProperty("id").GetInt32() == id);
        Assert.Equal(1, archived.GetProperty("interval").GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task Deleting_an_unknown_Chore_is_not_found()
    {
        HttpResponseMessage response = await _client.DeleteAsync("/api/chores/999999", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("DELETE", "/api/chores/1")]
    [InlineData("POST", "/api/chores/1/restore")]
    [InlineData("GET", "/api/chores?archived=true")]
    public async Task Deleting_and_restoring_need_the_household_password(string method, string path)
    {
        HttpResponseMessage response = await factory.CreateBrowserClient().SendAsync(
            new HttpRequestMessage(new HttpMethod(method), path), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<int> Create(string name, string? lastDone = null)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/chores/recurring", new { name, interval = new { count = 1, unit = "Week" }, lastDone }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        JsonElement created = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        return created.GetProperty("id").GetInt32();
    }

    private Task<HttpResponseMessage> Restore(int id) =>
        _client.PostAsync($"/api/chores/{id}/restore", null, TestContext.Current.CancellationToken);

    private async Task<List<JsonElement>> Chores(string path) =>
        [.. (await _client.GetFromJsonAsync<JsonElement>(path, TestContext.Current.CancellationToken)).EnumerateArray()];
}
