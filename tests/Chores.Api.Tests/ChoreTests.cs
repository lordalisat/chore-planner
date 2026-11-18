using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Chores.Api.Tests;

/// <summary>The Chore endpoints, against a clock fixed on Wednesday 10 March 2027.</summary>
public class ChoreTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateApiKeyClient();

    [Fact]
    public async Task A_new_Recurring_Chore_is_due_on_its_first_Preferred_Day()
    {
        HttpResponseMessage response = await Create(new
        {
            name = "Laundry",
            interval = new { count = 1, unit = "Week" },
            preferredDay = "Tuesday",
            importance = "Normal",
            effort = 30,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        JsonElement laundry = await Listed("Laundry");
        Assert.Equal("Recurring", laundry.GetProperty("kind").GetString());
        Assert.Equal("Normal", laundry.GetProperty("importance").GetString());
        Assert.Equal(30, laundry.GetProperty("effort").GetInt32());
        Assert.Equal(JsonValueKind.Null, laundry.GetProperty("area").ValueKind);
        Assert.Equal(1, laundry.GetProperty("interval").GetProperty("count").GetInt32());
        Assert.Equal("Week", laundry.GetProperty("interval").GetProperty("unit").GetString());
        Assert.Equal("Tuesday", laundry.GetProperty("preferredDay").GetString());
        Assert.Equal(JsonValueKind.Null, laundry.GetProperty("season").ValueKind);
        Assert.Equal("2027-03-16", laundry.GetProperty("dueDate").GetString());
        Assert.Equal(0, laundry.GetProperty("urgency").GetDouble());
        Assert.False(laundry.GetProperty("overdue").GetBoolean());
        Assert.False(laundry.GetProperty("dormant").GetBoolean());
        Assert.Equal(JsonValueKind.Null, laundry.GetProperty("lastDone").ValueKind);
    }

    [Fact]
    public async Task Last_done_gives_a_Chore_moved_over_from_elsewhere_its_Due_Date_at_once()
    {
        HttpResponseMessage response = await Create(new
        {
            name = "Change the bed",
            interval = new { count = 1, unit = "Week" },
            preferredDay = "Tuesday",
            importance = "High",
            lastDone = "2027-03-04", // a Thursday
        });

        JsonElement created = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        JsonElement listed = await Listed("Change the bed");
        Assert.Equal(listed.GetProperty("id").GetInt32(), created.GetProperty("id").GetInt32());
        Assert.Equal("2027-03-04", listed.GetProperty("lastDone").GetString());
        Assert.Equal("2027-03-09", listed.GetProperty("dueDate").GetString());
        Assert.True(listed.GetProperty("overdue").GetBoolean());
        // A day overdue is 1.25, times 1.5 for High Importance
        Assert.Equal(1.875, listed.GetProperty("urgency").GetDouble());
    }

    [Fact]
    public async Task A_Chore_outside_its_Season_is_Dormant()
    {
        await Create(new
        {
            name = "Mow the lawn",
            interval = new { count = 2, unit = "Week" },
            season = new { fromMonth = 4, toMonth = 10 },
            importance = "Normal",
        });

        JsonElement lawn = await Listed("Mow the lawn");
        Assert.True(lawn.GetProperty("dormant").GetBoolean());
        Assert.Equal(4, lawn.GetProperty("season").GetProperty("fromMonth").GetInt32());
        Assert.Equal(10, lawn.GetProperty("season").GetProperty("toMonth").GetInt32());
        // Not due before the Season starts
        Assert.Equal("2027-04-01", lawn.GetProperty("dueDate").GetString());
    }

    [Theory]
    [InlineData("preferredDay", 1, "Week", "FirstOfMonth", 1, 12)]
    [InlineData("preferredDay", 1, "Week", "Tuesdays", 1, 12)]
    [InlineData("interval", 0, "Week", null, 1, 12)]
    [InlineData("season", 1, "Week", null, 0, 12)]
    [InlineData("season", 1, "Week", null, 1, 13)]
    public async Task A_Chore_the_Domain_rejects_answers_a_validation_problem_at_that_field(
        string field, int count, string unit, string? preferredDay, int fromMonth, int toMonth)
    {
        HttpResponseMessage response = await Create(new
        {
            name = "Rejected",
            interval = new { count, unit },
            preferredDay,
            season = new { fromMonth, toMonth },
            importance = "Normal",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal([field], problem.GetProperty("errors").EnumerateObject().Select(e => e.Name));
    }

    [Fact]
    public async Task A_Chore_needs_a_Name()
    {
        HttpResponseMessage response = await Create(new { name = " ", interval = new { count = 1, unit = "Week" }, importance = "Normal" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.True(problem.GetProperty("errors").TryGetProperty("name", out _));
    }

    [Theory]
    [InlineData("GET", "/api/chores")]
    [InlineData("POST", "/api/chores/recurring")]
    public async Task Chores_need_the_household_password(string method, string path)
    {
        HttpResponseMessage response = await factory.CreateBrowserClient().SendAsync(
            new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { }) },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Task<HttpResponseMessage> Create(object chore) =>
        _client.PostAsJsonAsync("/api/chores/recurring", chore, TestContext.Current.CancellationToken);

    private async Task<JsonElement> Listed(string name)
    {
        JsonElement chores = await _client.GetFromJsonAsync<JsonElement>("/api/chores", TestContext.Current.CancellationToken);
        return Assert.Single(chores.EnumerateArray(), c => c.GetProperty("name").GetString() == name);
    }
}
