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

    [Fact]
    public async Task A_Chore_without_an_Importance_is_Normal()
    {
        await Create(new { name = "Dust", interval = new { count = 1, unit = "Week" } });

        Assert.Equal("Normal", (await Listed("Dust")).GetProperty("importance").GetString());
    }

    [Theory]
    [InlineData("preferredDay", 1, "Week", "FirstOfMonth", 1, 12)]
    [InlineData("preferredDay", 1, "Week", "FirstSaturday", 1, 12)]
    [InlineData("preferredDay", 1, "Month", "FifthSaturday", 1, 12)]
    [InlineData("preferredDay", 1, "Week", "Tuesdays", 1, 12)]
    [InlineData("preferredDay", 1, "Day", "Monday", 1, 12)]
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

    [Fact]
    public async Task Two_Chores_cannot_share_a_Name()
    {
        await Create(new { name = "Vacuum the stairs", interval = new { count = 1, unit = "Week" } });

        HttpResponseMessage response = await Create(new { name = " vacuum the STAIRS ", interval = new { count = 2, unit = "Week" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(["name"], problem.GetProperty("errors").EnumerateObject().Select(e => e.Name));
        Assert.Single((await _client.GetFromJsonAsync<JsonElement>("/api/chores", TestContext.Current.CancellationToken)).EnumerateArray(),
            c => c.GetProperty("name").GetString()!.Equals("Vacuum the stairs", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task One_Chore_is_read_by_its_id()
    {
        int id = await Created(new { name = "Water the plants", interval = new { count = 1, unit = "Week" }, preferredDay = "Friday" });

        JsonElement plants = await _client.GetFromJsonAsync<JsonElement>($"/api/chores/{id}", TestContext.Current.CancellationToken);

        Assert.Equal("Water the plants", plants.GetProperty("name").GetString());
        Assert.Equal("2027-03-12", plants.GetProperty("dueDate").GetString());
    }

    [Fact]
    public async Task An_unknown_Chore_is_not_found()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/chores/999999", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Editing_a_Recurring_Chore_replaces_its_fields_and_moves_its_Due_Date()
    {
        int id = await Created(new { name = "Wash the towels", interval = new { count = 1, unit = "Week" }, preferredDay = "Tuesday", effort = 30 });

        HttpResponseMessage response = await Edit(id, new
        {
            name = "Wash the bath towels",
            interval = new { count = 2, unit = "Week" },
            preferredDay = "Saturday",
            season = new { fromMonth = 3, toMonth = 10 },
            importance = "High",
            effort = (int?)null,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement towels = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(id, towels.GetProperty("id").GetInt32());
        Assert.Equal("Wash the bath towels", towels.GetProperty("name").GetString());
        Assert.Equal(2, towels.GetProperty("interval").GetProperty("count").GetInt32());
        Assert.Equal("Saturday", towels.GetProperty("preferredDay").GetString());
        Assert.Equal(3, towels.GetProperty("season").GetProperty("fromMonth").GetInt32());
        Assert.Equal("High", towels.GetProperty("importance").GetString());
        Assert.Equal(JsonValueKind.Null, towels.GetProperty("effort").ValueKind);
        // Never done, so due on the first Saturday from when it was created
        Assert.Equal("2027-03-13", towels.GetProperty("dueDate").GetString());
        Assert.Equal("Wash the bath towels", (await Listed("Wash the bath towels")).GetProperty("name").GetString());
    }

    [Fact]
    public async Task A_new_Interval_moves_the_Due_Date_on_from_Last_Done()
    {
        int id = await Created(new { name = "Clean the oven", interval = new { count = 1, unit = "Month" }, lastDone = "2027-03-01" });

        HttpResponseMessage response = await Edit(id, new { name = "Clean the oven", interval = new { count = 3, unit = "Month" } });

        JsonElement oven = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("2027-06-01", oven.GetProperty("dueDate").GetString());
        Assert.Equal("2027-03-01", oven.GetProperty("lastDone").GetString());
        Assert.Equal("Normal", oven.GetProperty("importance").GetString());
    }

    [Fact]
    public async Task A_weekday_in_the_month_goes_in_and_out_as_text()
    {
        await Create(new { name = "Check the smoke alarms", interval = new { count = 1, unit = "Month" }, preferredDay = "FirstSaturday" });

        Assert.Equal("FirstSaturday", (await Listed("Check the smoke alarms")).GetProperty("preferredDay").GetString());
    }

    [Fact]
    public async Task An_edit_the_Domain_rejects_answers_a_validation_problem_and_changes_nothing()
    {
        int id = await Created(new { name = "Pay the rent", interval = new { count = 1, unit = "Month" }, preferredDay = "FirstOfMonth" });

        HttpResponseMessage response = await Edit(id, new { name = "Pay the rent", interval = new { count = 1, unit = "Week" }, preferredDay = "FirstOfMonth" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(["preferredDay"], problem.GetProperty("errors").EnumerateObject().Select(e => e.Name));
        Assert.Equal("Month", (await Listed("Pay the rent")).GetProperty("interval").GetProperty("unit").GetString());
    }

    [Fact]
    public async Task An_edited_Chore_keeps_its_own_Name_but_cannot_take_another_ones()
    {
        await Created(new { name = "Defrost the freezer", interval = new { count = 6, unit = "Month" } });
        int id = await Created(new { name = "Descale the kettle", interval = new { count = 1, unit = "Month" } });

        HttpResponseMessage own = await Edit(id, new { name = "descale the kettle", interval = new { count = 2, unit = "Month" } });
        HttpResponseMessage other = await Edit(id, new { name = "Defrost the freezer", interval = new { count = 2, unit = "Month" } });

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, other.StatusCode);
        JsonElement problem = await other.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(["name"], problem.GetProperty("errors").EnumerateObject().Select(e => e.Name));
    }

    [Fact]
    public async Task Editing_a_One_off_Chore_replaces_its_Due_Date_and_ignores_a_schedule()
    {
        HttpResponseMessage created = await CreateOneOff(new { name = "Fix the gate", dueDate = "2027-03-20", effort = 60 });
        int id = (await created.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("id").GetInt32();

        HttpResponseMessage response = await Edit(id, new
        {
            name = "Fix the garden gate",
            dueDate = "2027-03-12",
            interval = new { count = 1, unit = "Week" },
            importance = "Low",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement gate = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("OneOff", gate.GetProperty("kind").GetString());
        Assert.Equal("Fix the garden gate", gate.GetProperty("name").GetString());
        Assert.Equal("2027-03-12", gate.GetProperty("dueDate").GetString());
        Assert.Equal("Low", gate.GetProperty("importance").GetString());
        Assert.Equal(JsonValueKind.Null, gate.GetProperty("effort").ValueKind);
        Assert.Equal(JsonValueKind.Null, gate.GetProperty("interval").ValueKind);
    }

    [Fact]
    public async Task Editing_an_unknown_Chore_is_not_found()
    {
        HttpResponseMessage response = await Edit(999999, new { name = "Nothing", interval = new { count = 1, unit = "Week" } });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_One_off_Chore_with_a_Due_Date_is_scheduled_by_it()
    {
        HttpResponseMessage response = await CreateOneOff(new
        {
            name = "Fix leaking tap",
            dueDate = "2027-03-12",
            importance = "High",
            effort = 45,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        JsonElement created = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        JsonElement tap = await Listed("Fix leaking tap");
        Assert.Equal(tap.GetProperty("id").GetInt32(), created.GetProperty("id").GetInt32());
        Assert.Equal("OneOff", tap.GetProperty("kind").GetString());
        Assert.Equal("High", tap.GetProperty("importance").GetString());
        Assert.Equal(45, tap.GetProperty("effort").GetInt32());
        Assert.Equal(JsonValueKind.Null, tap.GetProperty("interval").ValueKind);
        Assert.Equal("2027-03-12", tap.GetProperty("dueDate").GetString());
        // Two days before due is 0.5, times 1.5 for High Importance
        Assert.Equal(0.75, tap.GetProperty("urgency").GetDouble());
        Assert.False(tap.GetProperty("overdue").GetBoolean());
        Assert.False(tap.GetProperty("finished").GetBoolean());
    }

    [Fact]
    public async Task A_One_off_Chore_without_a_Due_Date_can_be_done_any_time()
    {
        HttpResponseMessage response = await CreateOneOff(new { name = "Sort the photos" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        JsonElement photos = await Listed("Sort the photos");
        Assert.Equal("Normal", photos.GetProperty("importance").GetString());
        Assert.Equal(JsonValueKind.Null, photos.GetProperty("dueDate").ValueKind);
        Assert.False(photos.GetProperty("overdue").GetBoolean());
        // Counts as two days before due
        Assert.Equal(0.5, photos.GetProperty("urgency").GetDouble());
    }

    [Fact]
    public async Task A_finished_One_off_Chore_is_still_listed()
    {
        HttpResponseMessage created = await CreateOneOff(new { name = "Hang the mirror", dueDate = "2027-03-08" });
        int id = (await created.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("id").GetInt32();
        (await _client.PostAsJsonAsync(
            $"/api/chores/{id}/complete", new { dueDate = "2027-03-08", date = "2027-03-09" }, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

        JsonElement mirror = await Listed("Hang the mirror");
        Assert.True(mirror.GetProperty("finished").GetBoolean());
        Assert.False(mirror.GetProperty("overdue").GetBoolean());
        Assert.Equal("2027-03-09", mirror.GetProperty("lastDone").GetString());
    }

    [Fact]
    public async Task A_One_off_Chore_cannot_take_the_Name_of_another_Chore()
    {
        await Create(new { name = "Clean the windows", interval = new { count = 3, unit = "Month" } });

        HttpResponseMessage response = await CreateOneOff(new { name = "clean the windows" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(["name"], problem.GetProperty("errors").EnumerateObject().Select(e => e.Name));
    }

    [Theory]
    [InlineData("GET", "/api/chores")]
    [InlineData("POST", "/api/chores/recurring")]
    [InlineData("POST", "/api/chores/one-off")]
    [InlineData("GET", "/api/chores/1")]
    [InlineData("PUT", "/api/chores/1")]
    public async Task Chores_need_the_household_password(string method, string path)
    {
        HttpResponseMessage response = await factory.CreateBrowserClient().SendAsync(
            new HttpRequestMessage(new HttpMethod(method), path) { Content = JsonContent.Create(new { }) },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Task<HttpResponseMessage> Create(object chore) =>
        _client.PostAsJsonAsync("/api/chores/recurring", chore, TestContext.Current.CancellationToken);

    private Task<HttpResponseMessage> CreateOneOff(object chore) =>
        _client.PostAsJsonAsync("/api/chores/one-off", chore, TestContext.Current.CancellationToken);

    [Fact]
    public async Task A_Description_goes_in_with_either_kind_and_comes_back_out()
    {
        await Create(new { name = "Scrub the bathtub", interval = new { count = 1, unit = "Week" }, description = "Descaler under the sink;\nrinse the plug hole too" });
        (await CreateOneOff(new { name = "Fix the dripping tap", description = "Washer is in the drawer" })).EnsureSuccessStatusCode();

        Assert.Equal("Descaler under the sink;\nrinse the plug hole too", (await Listed("Scrub the bathtub")).GetProperty("description").GetString());
        Assert.Equal("Washer is in the drawer", (await Listed("Fix the dripping tap")).GetProperty("description").GetString());
    }

    [Fact]
    public async Task An_edit_changes_the_Description_and_blank_clears_it()
    {
        int id = await Created(new { name = "Dust the bookshelves", interval = new { count = 1, unit = "Week" }, description = "Top shelves first" });

        (await Edit(id, new { name = "Dust the bookshelves", interval = new { count = 1, unit = "Week" }, description = "Microfibre cloth" })).EnsureSuccessStatusCode();
        Assert.Equal("Microfibre cloth", (await Listed("Dust the bookshelves")).GetProperty("description").GetString());

        (await Edit(id, new { name = "Dust the bookshelves", interval = new { count = 1, unit = "Week" }, description = "  " })).EnsureSuccessStatusCode();
        Assert.Equal(JsonValueKind.Null, (await Listed("Dust the bookshelves")).GetProperty("description").ValueKind);
    }

    [Fact]
    public async Task A_Description_over_2000_characters_answers_a_validation_problem_at_that_field()
    {
        HttpResponseMessage response = await Create(new { name = "Wipe the skirting boards", interval = new { count = 1, unit = "Week" }, description = new string('a', 2001) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(["description"], problem.GetProperty("errors").EnumerateObject().Select(e => e.Name));
    }

    private Task<HttpResponseMessage> Edit(int id, object chore) =>
        _client.PutAsJsonAsync($"/api/chores/{id}", chore, TestContext.Current.CancellationToken);

    private async Task<int> Created(object chore)
    {
        HttpResponseMessage response = await Create(chore);
        response.EnsureSuccessStatusCode();
        JsonElement created = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        return created.GetProperty("id").GetInt32();
    }

    private async Task<JsonElement> Listed(string name)
    {
        JsonElement chores = await _client.GetFromJsonAsync<JsonElement>("/api/chores", TestContext.Current.CancellationToken);
        return Assert.Single(chores.EnumerateArray(), c => c.GetProperty("name").GetString() == name);
    }
}
