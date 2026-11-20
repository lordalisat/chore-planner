using Chores.Api.Data;
using Chores.Api.Dtos;
using Chores.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Chores.Api;

/// <summary>Reading, creating, editing and deleting Areas.</summary>
public static class AreaEndpoints
{
    public static void MapAreaEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder areas = api.MapGroup("/areas");

        areas.MapGet("/", async (ChoresDbContext db) =>
        {
            List<Area> all = await AllAreas(db);
            Dictionary<int, int> choreCounts = await ChoreCounts(db);
            return TypedResults.Ok(all.OrderBy(a => a.Name).Select(area => ToDto(db, area, choreCounts)).ToList());
        });

        areas.MapPost("/", async Task<Results<Created<AreaDto>, ValidationProblem>> (SaveAreaRequest request, ChoresDbContext db) =>
        {
            List<Area> all = await AllAreas(db);
            Result<Area> result = request.ApplyTo(null, ParentOf(db, all, request));
            if (result.Value is not { } area)
            {
                return result.ToProblem();
            }

            db.Add(area);
            await db.SaveChangesAsync();
            AreaDto created = ToDto(db, area, new Dictionary<int, int>());
            return TypedResults.Created($"/api/areas/{created.Id}", created);
        });

        areas.MapPut("/{id:int}", async Task<Results<Ok<AreaDto>, NotFound, ValidationProblem>> (
            int id, SaveAreaRequest request, ChoresDbContext db) =>
        {
            List<Area> all = await AllAreas(db);
            if (all.SingleOrDefault(a => db.IdOf(a) == id) is not { } area)
            {
                return TypedResults.NotFound();
            }
            Result<Area> result = request.ApplyTo(area, ParentOf(db, all, request));
            if (result.Value is not { } saved)
            {
                return result.ToProblem();
            }

            await db.SaveChangesAsync();
            return TypedResults.Ok(ToDto(db, saved, await ChoreCounts(db)));
        });

        // Gone for good; its Chores, Archived ones too, and the Areas inside it stay, without it
        areas.MapDelete("/{id:int}", async Task<Results<NoContent, NotFound>> (int id, ChoresDbContext db) =>
        {
            List<Area> all = await AllAreas(db);
            if (all.SingleOrDefault(a => db.IdOf(a) == id) is not { } area)
            {
                return TypedResults.NotFound();
            }

            foreach (Chore chore in await db.Chores.IgnoreQueryFilters().Where(c => EF.Property<int?>(c, "AreaId") == id).ToListAsync())
            {
                chore.Area = null;
            }
            foreach (Area child in all.Where(a => a.Parent == area))
            {
                child.Parent = null;
            }
            db.Remove(area);
            await db.SaveChangesAsync();
            return TypedResults.NoContent();
        });
    }

    // Few enough to load at once, which also gives every Area its parents for the cycle check
    private static Task<List<Area>> AllAreas(ChoresDbContext db) => db.Areas.ToListAsync();

    // Archived Chores too, since deleting the Area detaches them as well
    private static Task<Dictionary<int, int>> ChoreCounts(ChoresDbContext db) => db.Chores.IgnoreQueryFilters()
        .Where(c => EF.Property<int?>(c, "AreaId") != null)
        .GroupBy(c => EF.Property<int>(c, "AreaId"))
        .ToDictionaryAsync(g => g.Key, g => g.Count());

    private static Area? ParentOf(ChoresDbContext db, List<Area> all, SaveAreaRequest request) =>
        request.ParentId is { } parentId ? all.SingleOrDefault(a => db.IdOf(a) == parentId) : null;

    private static AreaDto ToDto(ChoresDbContext db, Area area, IReadOnlyDictionary<int, int> choreCounts)
    {
        int id = db.IdOf(area);
        return new AreaDto(id, area.Name, area.Parent is null ? null : db.IdOf(area.Parent), choreCounts.GetValueOrDefault(id));
    }
}
