using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Chores.Api.Tests;

/// <summary>Items on Chores and ticking them, against a clock fixed on Wednesday 10 March 2027.</summary>
public class ItemTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateApiKeyClient();

    [Fact]
    public async Task Items_go_in_with_either_kind_in_order_and_come_back_with_ids()
    {
        JsonElement bathroom = await Created("/api/chores/recurring", new
        {
            name = "Clean the bathroom",
            interval = new { count = 1, unit = "Week" },
            items = new object[] { new { name = "Sink" }, new { name = "Toilet", description = "Blue cleaner,\nleave it 10 minutes" }, new { name = "Mirror" } },
        });
        JsonElement shelf = await Created("/api/chores/one-off", new { name = "Hang the shelf", items = new[] { new { name = "Drill" } } });

        Assert.Equal(["Sink", "Toilet", "Mirror"], Names(bathroom));
        Assert.Equal("Blue cleaner,\nleave it 10 minutes", bathroom.GetProperty("items")[1].GetProperty("description").GetString());
        Assert.All(bathroom.GetProperty("items").EnumerateArray(), item =>
        {
            Assert.True(item.GetProperty("id").GetInt32() > 0);
            Assert.False(item.GetProperty("ticked").GetBoolean());
        });
        Assert.Equal(["Drill"], Names(shelf));
    }

    [Fact]
    public async Task Ticking_and_unticking_an_Item_answers_the_Chore_and_records_no_Completion()
    {
        JsonElement chore = await Created("/api/chores/recurring", new
        {
            name = "Clean the kitchen",
            interval = new { count = 1, unit = "Week" },
            items = new[] { new { name = "Hob" }, new { name = "Counters" } },
        });
        int id = chore.GetProperty("id").GetInt32();
        int hob = ItemId(chore, 0);

        JsonElement ticked = await Ok(_client.PutAsJsonAsync($"/api/chores/{id}/items/{hob}", new { ticked = true }, Ct));
        Assert.True(ticked.GetProperty("items")[0].GetProperty("ticked").GetBoolean());
        Assert.False(ticked.GetProperty("items")[1].GetProperty("ticked").GetBoolean());
        Assert.Equal(0, ticked.GetProperty("completionCount").GetInt32());

        JsonElement unticked = await Ok(_client.PutAsJsonAsync($"/api/chores/{id}/items/{hob}", new { ticked = false }, Ct));
        Assert.False(unticked.GetProperty("items")[0].GetProperty("ticked").GetBoolean());
    }

    [Fact]
    public async Task Ticking_an_Item_the_Chore_does_not_have_is_not_found()
    {
        JsonElement chore = await Created("/api/chores/one-off", new { name = "Clean the windows" });

        HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/chores/{chore.GetProperty("id").GetInt32()}/items/9999", new { ticked = true }, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Doing_a_Recurring_Chore_unticks_its_Items()
    {
        JsonElement chore = await Created("/api/chores/recurring", new
        {
            name = "Hoover the stairs",
            interval = new { count = 1, unit = "Week" },
            items = new[] { new { name = "Treads" } },
        });
        int id = chore.GetProperty("id").GetInt32();
        await Ok(_client.PutAsJsonAsync($"/api/chores/{id}/items/{ItemId(chore, 0)}", new { ticked = true }, Ct));

        (await _client.PostAsJsonAsync($"/api/chores/{id}/complete", new { dueDate = chore.GetProperty("dueDate").GetString() }, Ct)).EnsureSuccessStatusCode();

        Assert.False((await Fetched(id)).GetProperty("items")[0].GetProperty("ticked").GetBoolean());
    }

    [Fact]
    public async Task An_edit_keeps_the_tick_of_every_Item_it_keeps_renamed_or_moved_and_new_ones_start_unticked()
    {
        JsonElement chore = await Created("/api/chores/recurring", new
        {
            name = "Clean the bedroom",
            interval = new { count = 1, unit = "Week" },
            items = new[] { new { name = "Bed" }, new { name = "Desk" }, new { name = "Floor" } },
        });
        int id = chore.GetProperty("id").GetInt32();
        int bed = ItemId(chore, 0), desk = ItemId(chore, 1);
        await Ok(_client.PutAsJsonAsync($"/api/chores/{id}/items/{bed}", new { ticked = true }, Ct));
        await Ok(_client.PutAsJsonAsync($"/api/chores/{id}/items/{desk}", new { ticked = true }, Ct));

        // Desk moved to the front, Bed renamed, Floor removed, Window new
        JsonElement edited = await Ok(_client.PutAsJsonAsync($"/api/chores/{id}", new
        {
            name = "Clean the bedroom",
            interval = new { count = 1, unit = "Week" },
            items = new object[] { new { id = desk, name = "Desk" }, new { id = bed, name = "Bed and pillows" }, new { name = "Window" } },
        }, Ct));

        Assert.Equal(["Desk", "Bed and pillows", "Window"], Names(edited));
        Assert.Equal([true, true, false], edited.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("ticked").GetBoolean()));
        // And so it was saved, in that order
        Assert.Equal(["Desk", "Bed and pillows", "Window"], Names(await Fetched(id)));
    }

    [Fact]
    public async Task An_edit_that_leaves_out_the_Items_keeps_them()
    {
        JsonElement chore = await Created("/api/chores/one-off", new { name = "Paint the fence", items = new[] { new { name = "Sand" } } });
        int id = chore.GetProperty("id").GetInt32();

        await Ok(_client.PutAsJsonAsync($"/api/chores/{id}", new { name = "Paint the garden fence" }, Ct));

        Assert.Equal(["Sand"], Names(await Fetched(id)));
    }

    public static TheoryData<object, string> ItemsTheDomainRejects => new()
    {
        { new[] { new { name = " " } }, "Every Item needs a name." },
        { new[] { new { name = "Sink" }, new { name = "sink" } }, "Two Items are called Sink." },
        { Enumerable.Range(1, 21).Select(n => new { name = $"Item {n}" }).ToArray(), "A Chore holds at most 20 Items." },
    };

    [Theory]
    [MemberData(nameof(ItemsTheDomainRejects))]
    public async Task Items_the_Domain_rejects_answer_a_validation_problem_at_items(object items, string message)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/chores/one-off", new { name = $"Rejected {Guid.NewGuid()}", items }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(message, Assert.Single(problem.GetProperty("errors").GetProperty("items").EnumerateArray()).GetString());
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string?[] Names(JsonElement chore) => [.. chore.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("name").GetString())];

    private static int ItemId(JsonElement chore, int index) => chore.GetProperty("items")[index].GetProperty("id").GetInt32();

    private async Task<JsonElement> Created(string path, object chore) => await Ok(_client.PostAsJsonAsync(path, chore, Ct));

    private async Task<JsonElement> Fetched(int id) => await _client.GetFromJsonAsync<JsonElement>($"/api/chores/{id}", Ct);

    private static async Task<JsonElement> Ok(Task<HttpResponseMessage> sent)
    {
        HttpResponseMessage response = await sent;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }
}
