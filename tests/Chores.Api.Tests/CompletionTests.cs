using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Chores.Api.Data;
using Chores.Domain;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Chores.Api.Tests;

/// <summary>Completing a Chore and undoing it, against a clock fixed on Wednesday 10 March 2027.</summary>
public class CompletionTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateApiKeyClient();

    [Fact]
    public async Task Completing_a_Chore_today_makes_it_due_an_Interval_from_today()
    {
        int id = await CreateWeekly("Water the plants"); // due today

        HttpResponseMessage response = await Complete(id, new { dueDate = "2027-03-10" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement answer = await Read(response);
        Assert.Equal(JsonValueKind.Number, answer.GetProperty("completionId").ValueKind);
        Assert.Equal("2027-03-10", answer.GetProperty("chore").GetProperty("lastDone").GetString());
        JsonElement listed = await Listed("Water the plants");
        Assert.Equal("2027-03-10", listed.GetProperty("lastDone").GetString());
        Assert.Equal("2027-03-17", listed.GetProperty("dueDate").GetString());
    }

    [Fact]
    public async Task Completing_a_Chore_on_an_earlier_day_makes_it_due_an_Interval_from_that_day()
    {
        int id = await CreateWeekly("Clean the windows");

        await Complete(id, new { dueDate = "2027-03-10", date = "2027-03-08" });

        JsonElement listed = await Listed("Clean the windows");
        Assert.Equal("2027-03-08", listed.GetProperty("lastDone").GetString());
        Assert.Equal("2027-03-15", listed.GetProperty("dueDate").GetString());
    }

    [Fact]
    public async Task Completing_a_Chore_whose_Due_Date_moved_on_is_a_conflict_that_records_nothing()
    {
        int id = await CreateWeekly("Descale the kettle");

        HttpResponseMessage response = await Complete(id, new { dueDate = "2027-03-03" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        JsonElement listed = await Listed("Descale the kettle");
        Assert.Equal(JsonValueKind.Null, listed.GetProperty("lastDone").ValueKind);
        Assert.Equal("2027-03-10", listed.GetProperty("dueDate").GetString());
    }

    [Fact]
    public async Task Undoing_a_Completion_restores_the_previous_Due_Date()
    {
        int id = await CreateWeekly("Change the bed", lastDone: "2027-03-04"); // due 11 March
        HttpResponseMessage completed = await Complete(id, new { dueDate = "2027-03-11" });
        int completionId = (await Read(completed)).GetProperty("completionId").GetInt32();

        HttpResponseMessage response = await _client.DeleteAsync($"/api/chores/{id}/completions/{completionId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("2027-03-11", (await Read(response)).GetProperty("dueDate").GetString());
        JsonElement listed = await Listed("Change the bed");
        Assert.Equal("2027-03-04", listed.GetProperty("lastDone").GetString());
        Assert.Equal("2027-03-11", listed.GetProperty("dueDate").GetString());
    }

    [Fact]
    public async Task Undoing_a_Completion_of_another_Chore_is_not_found()
    {
        int done = await CreateWeekly("Wipe the hob");
        int other = await CreateWeekly("Wipe the fridge");
        HttpResponseMessage completed = await Complete(done, new { dueDate = "2027-03-10" });
        int completionId = (await Read(completed)).GetProperty("completionId").GetInt32();

        HttpResponseMessage response = await _client.DeleteAsync($"/api/chores/{other}/completions/{completionId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("2027-03-10", (await Listed("Wipe the hob")).GetProperty("lastDone").GetString());
    }

    [Fact]
    public async Task A_completed_One_off_Chore_is_finished_and_cannot_be_completed_again()
    {
        int id = await CreateOneOff("Fix the leaking tap");

        HttpResponseMessage first = await Complete(id, new { dueDate = (string?)null });
        HttpResponseMessage second = await Complete(id, new { dueDate = (string?)null });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        JsonElement listed = await Listed("Fix the leaking tap");
        Assert.True(listed.GetProperty("finished").GetBoolean());
        Assert.Equal(JsonValueKind.Null, listed.GetProperty("urgency").ValueKind);
    }

    [Fact]
    public async Task A_Chore_cannot_be_done_in_the_future()
    {
        int id = await CreateWeekly("Sweep the porch");

        HttpResponseMessage response = await Complete(id, new { dueDate = "2027-03-10", date = "2027-03-11" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["date"], (await Read(response)).GetProperty("errors").EnumerateObject().Select(e => e.Name));
        Assert.Equal(JsonValueKind.Null, (await Listed("Sweep the porch")).GetProperty("lastDone").ValueKind);
    }

    [Theory]
    [InlineData("POST", "/api/chores/999/complete")]
    [InlineData("DELETE", "/api/chores/999/completions/1")]
    public async Task Completing_or_undoing_for_an_unknown_Chore_is_not_found(string method, string path)
    {
        HttpResponseMessage response = await _client.SendAsync(
            new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { dueDate = "2027-03-10" }) },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/chores/1/complete")]
    [InlineData("DELETE", "/api/chores/1/completions/1")]
    public async Task Completing_and_undoing_need_the_household_password(string method, string path)
    {
        HttpResponseMessage response = await factory.CreateBrowserClient().SendAsync(
            new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { }) },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // There is no endpoint for creating One-off Chores yet
    private async Task<int> CreateOneOff(string name)
    {
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            ChoresDbContext db = scope.ServiceProvider.GetRequiredService<ChoresDbContext>();
            db.Add(new OneOffChore(name, ApiFactory.Today));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        return (await Listed(name)).GetProperty("id").GetInt32();
    }

    private async Task<int> CreateWeekly(string name, string? lastDone = null)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/chores/recurring", new { name, interval = new { count = 1, unit = "Week" }, lastDone }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await Read(response)).GetProperty("id").GetInt32();
    }

    private Task<HttpResponseMessage> Complete(int id, object request) =>
        _client.PostAsJsonAsync($"/api/chores/{id}/complete", request, TestContext.Current.CancellationToken);

    private static Task<JsonElement> Read(HttpResponseMessage response) =>
        response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

    private async Task<JsonElement> Listed(string name)
    {
        JsonElement chores = await _client.GetFromJsonAsync<JsonElement>("/api/chores", TestContext.Current.CancellationToken);
        return Assert.Single(chores.EnumerateArray(), c => c.GetProperty("name").GetString() == name);
    }
}
