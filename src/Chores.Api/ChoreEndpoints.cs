using Chores.Api.Data;
using Chores.Api.Dtos;
using Chores.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Chores.Api;

/// <summary>Reading, creating and editing Chores; scheduling is computed here with the server clock, never by the front end.</summary>
public static class ChoreEndpoints
{
    public static void MapChoreEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder chores = api.MapGroup("/chores");

        chores.MapGet("/", async (ChoresDbContext db, TimeProvider clock) =>
        {
            List<Chore> all = await db.Chores.Include(c => c.Area).ToListAsync();
            return TypedResults.Ok(all.Select(chore => ChoreDto.From(chore, entity => IdOf(db, entity), clock)).ToList());
        });

        chores.MapGet("/{id:int}", async Task<Results<Ok<ChoreDto>, NotFound>> (int id, ChoresDbContext db, TimeProvider clock) =>
            await Find(db, id) is { } chore
                ? TypedResults.Ok(ChoreDto.From(chore, entity => IdOf(db, entity), clock))
                : TypedResults.NotFound());

        chores.MapPost("/recurring", async Task<Results<Created<ChoreDto>, ValidationProblem>> (
            CreateRecurringChoreRequest request, ChoresDbContext db, TimeProvider clock) =>
        {
            List<string> takenNames = await db.Chores.Select(c => c.Name).ToListAsync();
            Result<RecurringChore> result = request.ToChore(clock.Today(), takenNames);
            if (result.Value is not { } chore)
            {
                return result.ToProblem();
            }

            db.Add(chore);
            await db.SaveChangesAsync();
            ChoreDto created = ChoreDto.From(chore, entity => IdOf(db, entity), clock);
            return TypedResults.Created($"/api/chores/{created.Id}", created);
        });

        chores.MapPut("/{id:int}", async Task<Results<Ok<ChoreDto>, NotFound, ValidationProblem>> (
            int id, EditChoreRequest request, ChoresDbContext db, TimeProvider clock) =>
        {
            if (await Find(db, id) is not { } chore)
            {
                return TypedResults.NotFound();
            }
            List<string> takenNames = await db.Chores.Where(c => EF.Property<int>(c, "Id") != id).Select(c => c.Name).ToListAsync();
            Result<Chore> result = request.ApplyTo(chore, takenNames);
            if (result.Value is null)
            {
                return result.ToProblem();
            }

            await db.SaveChangesAsync();
            return TypedResults.Ok(ChoreDto.From(chore, entity => IdOf(db, entity), clock));
        });
    }

    private static Task<Chore?> Find(ChoresDbContext db, int id) =>
        db.Chores.Include(c => c.Area).SingleOrDefaultAsync(c => EF.Property<int>(c, "Id") == id);

    // Keys are shadow properties, see ChoresDbContext
    private static int IdOf(ChoresDbContext db, object entity) => (int)db.Entry(entity).Property("Id").CurrentValue!;
}
