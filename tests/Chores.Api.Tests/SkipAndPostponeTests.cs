using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Chores.Api.Tests;

/// <summary>Skipping and postponing a Chore, against a clock fixed on Wednesday 10 March 2027.</summary>
public class SkipAndPostponeTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateApiKeyClient();

    [Fact]
    public async Task Skipping_a_Chore_moves_its_Due_Date_on_and_leaves_Last_Done_as_it_is()
    {
        int id = await CreateWeekly("Do the laundry", lastDone: "2027-03-02"); // due 9 March

        HttpResponseMessage response = await Post(id, "skip", new { dueDate = "2027-03-09" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement answer = await Read(response);
        Assert.Equal(JsonValueKind.Number, answer.GetProperty("completionId").ValueKind);
        Assert.Equal("2027-03-16", answer.GetProperty("chore").GetProperty("dueDate").GetString());
        JsonElement listed = await Listed("Do the laundry");
        Assert.Equal("2027-03-02", listed.GetProperty("lastDone").GetString());
        Assert.Equal("2027-03-16", listed.GetProperty("dueDate").GetString());
    }

    [Fact]
    public async Task Undoing_a_Skip_brings_the_Due_Date_back()
    {
        int id = await CreateWeekly("Hoover the stairs", lastDone: "2027-03-02");
        HttpResponseMessage skipped = await Post(id, "skip", new { dueDate = "2027-03-09" });
        int completionId = (await Read(skipped)).GetProperty("completionId").GetInt32();

        HttpResponseMessage response = await _client.DeleteAsync($"/api/chores/{id}/completions/{completionId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("2027-03-09", (await Listed("Hoover the stairs")).GetProperty("dueDate").GetString());
    }

    [Fact]
    public async Task Skipping_a_One_off_Chore_is_a_bad_request()
    {
        int id = await CreateOneOff("Fix the doorbell", dueDate: "2027-03-12");

        HttpResponseMessage response = await Post(id, "skip", new { dueDate = "2027-03-12" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False((await Listed("Fix the doorbell")).GetProperty("finished").GetBoolean());
    }

    [Fact]
    public async Task Postponing_a_Chore_sets_its_Due_Date_until_the_next_Completion()
    {
        int id = await CreateWeekly("Mow the lawn"); // due today

        HttpResponseMessage response = await Post(id, "postpone", new { dueDate = "2027-03-10", to = "2027-03-13" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("2027-03-13", (await Read(response)).GetProperty("dueDate").GetString());
        Assert.Equal("2027-03-13", (await Listed("Mow the lawn")).GetProperty("dueDate").GetString());

        (await Post(id, "complete", new { dueDate = "2027-03-13" })).EnsureSuccessStatusCode();

        Assert.Equal("2027-03-17", (await Listed("Mow the lawn")).GetProperty("dueDate").GetString());
    }

    [Fact]
    public async Task Postponing_a_One_off_Chore_sets_its_Due_Date()
    {
        int id = await CreateOneOff("Return the library books", dueDate: null);

        HttpResponseMessage response = await Post(id, "postpone", new { dueDate = (string?)null, to = "2027-03-20" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("2027-03-20", (await Listed("Return the library books")).GetProperty("dueDate").GetString());
    }

    [Theory]
    [InlineData("2027-03-10")]
    [InlineData("2027-03-09")]
    public async Task Postponing_to_today_or_earlier_is_a_bad_request(string to)
    {
        int id = await CreateWeekly($"Clean the gutters {to}");

        HttpResponseMessage response = await Post(id, "postpone", new { dueDate = "2027-03-10", to });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["to"], (await Read(response)).GetProperty("errors").EnumerateObject().Select(e => e.Name));
        Assert.Equal("2027-03-10", (await Listed($"Clean the gutters {to}")).GetProperty("dueDate").GetString());
    }

    [Fact]
    public async Task Skipping_a_Chore_whose_Due_Date_moved_on_is_a_conflict_that_records_nothing()
    {
        int id = await CreateWeekly("Wash the car");

        HttpResponseMessage response = await Post(id, "skip", new { dueDate = "2027-03-03" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        JsonElement listed = await Listed("Wash the car");
        Assert.Equal(0, listed.GetProperty("completionCount").GetInt32());
        Assert.Equal("2027-03-10", listed.GetProperty("dueDate").GetString());
    }

    [Fact]
    public async Task Postponing_a_Chore_whose_Due_Date_moved_on_is_a_conflict_that_changes_nothing()
    {
        int id = await CreateWeekly("Feed the fish");

        HttpResponseMessage response = await Post(id, "postpone", new { dueDate = "2027-03-03", to = "2027-03-13" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("2027-03-10", (await Listed("Feed the fish")).GetProperty("dueDate").GetString());
    }

    [Theory]
    [InlineData("/api/chores/999/skip")]
    [InlineData("/api/chores/999/postpone")]
    public async Task Skipping_or_postponing_an_unknown_Chore_is_not_found(string path)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(path, new { dueDate = "2027-03-10", to = "2027-03-13" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/chores/1/skip")]
    [InlineData("/api/chores/1/postpone")]
    public async Task Skipping_and_postponing_need_the_household_password(string path)
    {
        HttpResponseMessage response = await factory.CreateBrowserClient().PostAsJsonAsync(path, new { }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<int> CreateOneOff(string name, string? dueDate)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/chores/one-off", new { name, dueDate }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await Read(response)).GetProperty("id").GetInt32();
    }

    private async Task<int> CreateWeekly(string name, string? lastDone = null)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/chores/recurring", new { name, interval = new { count = 1, unit = "Week" }, lastDone }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await Read(response)).GetProperty("id").GetInt32();
    }

    private Task<HttpResponseMessage> Post(int id, string action, object request) =>
        _client.PostAsJsonAsync($"/api/chores/{id}/{action}", request, TestContext.Current.CancellationToken);

    private static Task<JsonElement> Read(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

    private async Task<JsonElement> Listed(string name)
    {
        JsonElement chores = await _client.GetFromJsonAsync<JsonElement>("/api/chores", TestContext.Current.CancellationToken);
        return Assert.Single(chores.EnumerateArray(), c => c.GetProperty("name").GetString() == name);
    }
}
