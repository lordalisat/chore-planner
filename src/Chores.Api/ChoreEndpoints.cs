using Chores.Api.Data;
using Chores.Api.Dtos;
using Chores.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Chores.Api;

/// <summary>Reading, creating and deleting Chores; scheduling is computed here with the server clock, never by the front end.</summary>
public static class ChoreEndpoints
{
    public static void MapChoreEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder chores = api.MapGroup("/chores");

        // Archived Chores only when asked for, and then only those
        chores.MapGet("/", async (ChoresDbContext db, TimeProvider clock, bool archived = false) =>
        {
            IQueryable<Chore> query = archived ? db.Chores.IgnoreQueryFilters().Where(c => c.ArchivedOn != null) : db.Chores;
            List<Chore> all = await query.Include(c => c.Area).ToListAsync();
            return TypedResults.Ok(all.Select(chore => ChoreDto.From(chore, entity => IdOf(db, entity), clock)).ToList());
        });

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
            // No address yet: there is no endpoint for one Chore
            return TypedResults.Created((string?)null, ChoreDto.From(chore, entity => IdOf(db, entity), clock));
        });

        chores.MapPost("/one-off", async Task<Results<Created<ChoreDto>, ValidationProblem>> (
            CreateOneOffChoreRequest request, ChoresDbContext db, TimeProvider clock) =>
        {
            List<string> takenNames = await db.Chores.Select(c => c.Name).ToListAsync();
            Result<OneOffChore> result = request.ToChore(clock.Today(), takenNames);
            if (result.Value is not { } chore)
            {
                return result.ToProblem();
            }

            db.Add(chore);
            await db.SaveChangesAsync();
            return TypedResults.Created((string?)null, ChoreDto.From(chore, entity => IdOf(db, entity), clock));
        });

        // Keeps the history of a Chore that has any; an Archived Chore is not found, so it is never removed for good
        chores.MapDelete("/{id:int}", async Task<Results<Ok<DeleteChoreResponse>, NotFound>> (int id, ChoresDbContext db, TimeProvider clock) =>
        {
            if (await db.Chores.SingleOrDefaultAsync(c => EF.Property<int>(c, "Id") == id) is not { } chore)
            {
                return TypedResults.NotFound();
            }

            bool archive = chore.Completions.Count > 0;
            if (archive)
            {
                chore.Archive(clock.Today());
            }
            else
            {
                db.Remove(chore);
            }
            await db.SaveChangesAsync();
            return TypedResults.Ok(new DeleteChoreResponse(archive));
        });

        chores.MapPost("/{id:int}/restore", async Task<Results<Ok<ChoreDto>, NotFound>> (int id, ChoresDbContext db, TimeProvider clock) =>
        {
            if (await db.Chores.IgnoreQueryFilters().Include(c => c.Area)
                    .SingleOrDefaultAsync(c => EF.Property<int>(c, "Id") == id && c.ArchivedOn != null) is not { } chore)
            {
                return TypedResults.NotFound();
            }

            chore.Restore();
            await db.SaveChangesAsync();
            return TypedResults.Ok(ChoreDto.From(chore, entity => IdOf(db, entity), clock));
        });
    }

    // Keys are shadow properties, see ChoresDbContext
    private static int IdOf(ChoresDbContext db, object entity) => (int)db.Entry(entity).Property("Id").CurrentValue!;
}
