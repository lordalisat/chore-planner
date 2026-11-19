using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Chores.Api.Tests;

/// <summary>The Area endpoints, and Chores using an Area.</summary>
public class AreaTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateApiKeyClient();

    [Fact]
    public async Task An_Area_is_created_inside_a_parent()
    {
        int downstairs = await Create("Downstairs");

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/areas", new { name = " Kitchen ", parentId = downstairs }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        JsonElement kitchen = await Listed(await IdOf(response));
        Assert.Equal("Kitchen", kitchen.GetProperty("name").GetString());
        Assert.Equal(downstairs, kitchen.GetProperty("parentId").GetInt32());
        Assert.Equal(0, kitchen.GetProperty("choreCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, (await Listed(downstairs)).GetProperty("parentId").ValueKind);
    }

    [Fact]
    public async Task An_Area_needs_a_name_and_a_known_parent()
    {
        HttpResponseMessage unnamed = await _client.PostAsJsonAsync("/api/areas", new { name = " " }, TestContext.Current.CancellationToken);
        HttpResponseMessage orphan = await _client.PostAsJsonAsync(
            "/api/areas", new { name = "Attic", parentId = 999_999 }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, unnamed.StatusCode);
        Assert.Contains("\"name\"", await unnamed.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.BadRequest, orphan.StatusCode);
        Assert.Contains("\"parentId\"", await orphan.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task An_Area_is_renamed_and_moved_to_another_parent_or_to_the_top()
    {
        int downstairs = await Create("Downstairs");
        int upstairs = await Create("Upstairs");
        int bathroom = await Create("Bathroom", downstairs);

        HttpResponseMessage moved = await Edit(bathroom, new { name = "Big bathroom", parentId = upstairs });

        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        JsonElement listed = await Listed(bathroom);
        Assert.Equal("Big bathroom", listed.GetProperty("name").GetString());
        Assert.Equal(upstairs, listed.GetProperty("parentId").GetInt32());

        await Edit(bathroom, new { name = "Big bathroom" });

        Assert.Equal(JsonValueKind.Null, (await Listed(bathroom)).GetProperty("parentId").ValueKind);
    }

    [Fact]
    public async Task An_Area_cannot_sit_inside_itself_or_an_Area_inside_it()
    {
        int house = await Create("House");
        int garden = await Create("Garden", house);
        int shed = await Create("Shed", garden);

        HttpResponseMessage intoChild = await Edit(house, new { name = "House", parentId = shed });
        HttpResponseMessage intoItself = await Edit(garden, new { name = "Garden", parentId = garden });

        Assert.Equal(HttpStatusCode.BadRequest, intoChild.StatusCode);
        Assert.Contains("\"parentId\"", await intoChild.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.BadRequest, intoItself.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await Listed(house)).GetProperty("parentId").ValueKind);
        Assert.Equal(house, (await Listed(garden)).GetProperty("parentId").GetInt32());
    }

    [Fact]
    public async Task Editing_or_deleting_an_unknown_Area_is_not_found()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Edit(999_999, new { name = "Loft" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Delete(999_999)).StatusCode);
    }

    [Fact]
    public async Task A_Chore_belongs_to_an_Area_it_is_created_or_edited_with()
    {
        int kitchen = await Create("Kitchen");
        int utility = await Create("Utility room");

        HttpResponseMessage created = await _client.PostAsJsonAsync(
            "/api/chores/recurring", new { name = "Clean the fridge", interval = new { count = 1, unit = "Month" }, areaId = kitchen },
            TestContext.Current.CancellationToken);
        int fridge = await IdOf(created);
        HttpResponseMessage oneOff = await _client.PostAsJsonAsync(
            "/api/chores/one-off", new { name = "Fix the kitchen tap", areaId = kitchen }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("Kitchen", (await Chore(fridge)).GetProperty("area").GetProperty("name").GetString());
        Assert.Equal(kitchen, (await Chore(await IdOf(oneOff))).GetProperty("area").GetProperty("id").GetInt32());
        Assert.Equal(2, (await Listed(kitchen)).GetProperty("choreCount").GetInt32());

        await EditChore(fridge, new { name = "Clean the fridge", interval = new { count = 1, unit = "Month" }, areaId = utility });
        Assert.Equal("Utility room", (await Chore(fridge)).GetProperty("area").GetProperty("name").GetString());

        await EditChore(fridge, new { name = "Clean the fridge", interval = new { count = 1, unit = "Month" } });
        Assert.Equal(JsonValueKind.Null, (await Chore(fridge)).GetProperty("area").ValueKind);
    }

    [Fact]
    public async Task A_Chore_with_an_unknown_Area_is_rejected()
    {
        HttpResponseMessage recurring = await _client.PostAsJsonAsync(
            "/api/chores/recurring", new { name = "Mop the hall", interval = new { count = 1, unit = "Week" }, areaId = 999_999 },
            TestContext.Current.CancellationToken);
        HttpResponseMessage oneOff = await _client.PostAsJsonAsync(
            "/api/chores/one-off", new { name = "Paint the hall", areaId = 999_999 }, TestContext.Current.CancellationToken);
        HttpResponseMessage created = await _client.PostAsJsonAsync(
            "/api/chores/one-off", new { name = "Hang the hall mirror" }, TestContext.Current.CancellationToken);
        HttpResponseMessage edit = await EditChore(await IdOf(created), new { name = "Hang the hall mirror", areaId = 999_999 });

        Assert.Equal(HttpStatusCode.BadRequest, recurring.StatusCode);
        Assert.Contains("\"areaId\"", await recurring.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.BadRequest, oneOff.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, edit.StatusCode);
        Assert.Contains("\"areaId\"", await edit.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Deleting_an_Area_detaches_its_Chores_Archived_ones_too_and_the_Areas_inside_it()
    {
        int downstairs = await Create("Ground floor");
        int hall = await Create("Hall", downstairs);
        int lounge = await Create("Lounge", downstairs);
        int dust = await IdOf(await _client.PostAsJsonAsync(
            "/api/chores/one-off", new { name = "Dust the ground floor", areaId = downstairs }, TestContext.Current.CancellationToken));
        int windows = await IdOf(await _client.PostAsJsonAsync(
            "/api/chores/recurring",
            new { name = "Wash the ground floor windows", interval = new { count = 1, unit = "Month" }, lastDone = "2027-03-01", areaId = downstairs },
            TestContext.Current.CancellationToken));
        await _client.DeleteAsync($"/api/chores/{windows}", TestContext.Current.CancellationToken);
        // The confirmation names every Chore deleting it detaches
        Assert.Equal(2, (await Listed(downstairs)).GetProperty("choreCount").GetInt32());

        HttpResponseMessage response = await Delete(downstairs);

        Assert.True(response.IsSuccessStatusCode);
        Assert.DoesNotContain(await Areas(), a => a.GetProperty("id").GetInt32() == downstairs);
        Assert.Equal(JsonValueKind.Null, (await Listed(hall)).GetProperty("parentId").ValueKind);
        Assert.Equal(JsonValueKind.Null, (await Listed(lounge)).GetProperty("parentId").ValueKind);
        Assert.Equal(JsonValueKind.Null, (await Chore(dust)).GetProperty("area").ValueKind);
        JsonElement archived = Assert.Single(
            (await _client.GetFromJsonAsync<JsonElement>("/api/chores?archived=true", TestContext.Current.CancellationToken)).EnumerateArray(),
            c => c.GetProperty("id").GetInt32() == windows);
        Assert.Equal(JsonValueKind.Null, archived.GetProperty("area").ValueKind);
    }

    [Theory]
    [InlineData("GET", "/api/areas")]
    [InlineData("POST", "/api/areas")]
    [InlineData("PUT", "/api/areas/1")]
    [InlineData("DELETE", "/api/areas/1")]
    public async Task Areas_need_the_household_password(string method, string path)
    {
        HttpResponseMessage response = await factory.CreateBrowserClient().SendAsync(
            new HttpRequestMessage(new HttpMethod(method), path), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Task<HttpResponseMessage> Edit(int id, object area) =>
        _client.PutAsJsonAsync($"/api/areas/{id}", area, TestContext.Current.CancellationToken);

    private Task<HttpResponseMessage> Delete(int id) => _client.DeleteAsync($"/api/areas/{id}", TestContext.Current.CancellationToken);

    private Task<HttpResponseMessage> EditChore(int id, object chore) =>
        _client.PutAsJsonAsync($"/api/chores/{id}", chore, TestContext.Current.CancellationToken);

    private Task<JsonElement> Chore(int id) =>
        _client.GetFromJsonAsync<JsonElement>($"/api/chores/{id}", TestContext.Current.CancellationToken);

    private async Task<int> Create(string name, int? parentId = null)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/areas", new { name, parentId }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await IdOf(response);
    }

    private static async Task<int> IdOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("id").GetInt32();

    private async Task<List<JsonElement>> Areas() =>
        [.. (await _client.GetFromJsonAsync<JsonElement>("/api/areas", TestContext.Current.CancellationToken)).EnumerateArray()];

    private async Task<JsonElement> Listed(int id) => Assert.Single(await Areas(), a => a.GetProperty("id").GetInt32() == id);
}
