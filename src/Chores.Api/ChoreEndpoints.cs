using Chores.Api.Data;
using Chores.Api.Dtos;
using Chores.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Chores.Api;

/// <summary>Reading and creating Chores; scheduling is computed here with the server clock, never by the front end.</summary>
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

        chores.MapPost("/recurring", async Task<Results<Created<ChoreDto>, ValidationProblem>> (
            CreateRecurringChoreRequest request, ChoresDbContext db, TimeProvider clock) =>
        {
            var errors = new FieldErrors();
            List<string> takenNames = await db.Chores.Select(c => c.Name).ToListAsync();
            if (request.ToChore(clock.Today(), takenNames, errors) is not { } chore)
            {
                return errors.ToProblem();
            }

            db.Add(chore);
            await db.SaveChangesAsync();
            // No address yet: there is no endpoint for one Chore
            return TypedResults.Created((string?)null, ChoreDto.From(chore, entity => IdOf(db, entity), clock));
        });
    }

    // Keys are shadow properties, see ChoresDbContext
    private static int IdOf(ChoresDbContext db, object entity) => (int)db.Entry(entity).Property("Id").CurrentValue!;
}
