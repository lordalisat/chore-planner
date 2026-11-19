using Chores.Api.Data;
using Chores.Api.Dtos;
using Chores.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Chores.Api;

/// <summary>Reading, creating and completing Chores; scheduling is computed here with the server clock, never by the front end.</summary>
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

        chores.MapPost("/{id:int}/complete", async Task<Results<Ok<RecordedCompletionDto>, NotFound, Conflict, ValidationProblem>> (
            int id, CompleteChoreRequest request, ChoresDbContext db, TimeProvider clock) =>
        {
            if (await Find(db, id) is not { } chore)
            {
                return TypedResults.NotFound();
            }
            if (!IsCurrent(chore, request.DueDate))
            {
                return TypedResults.Conflict();
            }

            Result<Chore> result = request.CompleteOn(chore, clock.Today());
            if (result.Value is not { } completed)
            {
                return result.ToProblem();
            }

            await db.SaveChangesAsync();
            return TypedResults.Ok(new RecordedCompletionDto(
                IdOf(db, completed.Completions[^1]), ChoreDto.From(completed, entity => IdOf(db, entity), clock)));
        });

        // Undo for a Completion, done or skipped
        chores.MapDelete("/{id:int}/completions/{completionId:int}", async Task<Results<Ok<ChoreDto>, NotFound>> (
            int id, int completionId, ChoresDbContext db, TimeProvider clock) =>
        {
            if (await Find(db, id) is not { } chore
                || chore.Completions.FirstOrDefault(c => IdOf(db, c) == completionId) is not { } completion)
            {
                return TypedResults.NotFound();
            }

            chore.RemoveCompletion(completion);
            await db.SaveChangesAsync();
            return TypedResults.Ok(ChoreDto.From(chore, entity => IdOf(db, entity), clock));
        });
    }

    /// <summary>Whether the Due Date a device saw is still the Chore's: not when someone else acted on it first.</summary>
    private static bool IsCurrent(Chore chore, DateOnly? dueDate) => chore switch
    {
        RecurringChore recurring => recurring.DueDate == dueDate,
        OneOffChore oneOff => !oneOff.IsFinished && oneOff.DueDate == dueDate,
        _ => false,
    };

    private static Task<Chore?> Find(ChoresDbContext db, int id) =>
        db.Chores.Include(c => c.Area).SingleOrDefaultAsync(c => EF.Property<int>(c, "Id") == id);

    // Keys are shadow properties, see ChoresDbContext
    private static int IdOf(ChoresDbContext db, object entity) => (int)db.Entry(entity).Property("Id").CurrentValue!;
}
