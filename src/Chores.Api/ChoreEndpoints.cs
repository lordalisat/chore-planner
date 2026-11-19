using Chores.Api.Data;
using Chores.Api.Dtos;
using Chores.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Chores.Api;

/// <summary>Reading, creating, editing, deleting and completing Chores; scheduling is computed here with the server clock, never by the front end.</summary>
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

        chores.MapGet("/{id:int}", async Task<Results<Ok<ChoreDto>, NotFound>> (int id, ChoresDbContext db, TimeProvider clock) =>
            await Find(db, id) is { } chore
                ? TypedResults.Ok(ChoreDto.From(chore, entity => IdOf(db, entity), clock))
                : TypedResults.NotFound());

        chores.MapPost("/recurring", async Task<Results<Created<ChoreDto>, ValidationProblem>> (
            CreateRecurringChoreRequest request, ChoresDbContext db, TimeProvider clock) =>
        {
            List<string> takenNames = await db.Chores.Select(c => c.Name).ToListAsync();
            Result<RecurringChore> result = request.ToChore(clock.Today(), takenNames, await FindArea(db, request.AreaId));
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
            Result<Chore> result = request.ApplyTo(chore, takenNames, await FindArea(db, request.AreaId));
            if (result.Value is null)
            {
                return result.ToProblem();
            }

            await db.SaveChangesAsync();
            return TypedResults.Ok(ChoreDto.From(chore, entity => IdOf(db, entity), clock));
        });

        chores.MapPost("/one-off", async Task<Results<Created<ChoreDto>, ValidationProblem>> (
            CreateOneOffChoreRequest request, ChoresDbContext db, TimeProvider clock) =>
        {
            List<string> takenNames = await db.Chores.Select(c => c.Name).ToListAsync();
            Result<OneOffChore> result = request.ToChore(clock.Today(), takenNames, await FindArea(db, request.AreaId));
            if (result.Value is not { } chore)
            {
                return result.ToProblem();
            }

            db.Add(chore);
            await db.SaveChangesAsync();
            ChoreDto created = ChoreDto.From(chore, entity => IdOf(db, entity), clock);
            return TypedResults.Created($"/api/chores/{created.Id}", created);
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

        chores.MapPost("/{id:int}/skip", async Task<Results<Ok<RecordedCompletionDto>, NotFound, Conflict, ValidationProblem>> (
            int id, SkipChoreRequest request, ChoresDbContext db, TimeProvider clock) =>
        {
            if (await Find(db, id) is not { } chore)
            {
                return TypedResults.NotFound();
            }
            if (!IsCurrent(chore, request.DueDate))
            {
                return TypedResults.Conflict();
            }

            Result<Chore> result = request.SkipOn(chore, clock.Today());
            if (result.Value is not { } skipped)
            {
                return result.ToProblem();
            }

            await db.SaveChangesAsync();
            return TypedResults.Ok(new RecordedCompletionDto(
                IdOf(db, skipped.Completions[^1]), ChoreDto.From(skipped, entity => IdOf(db, entity), clock)));
        });

        chores.MapPost("/{id:int}/postpone", async Task<Results<Ok<ChoreDto>, NotFound, Conflict, ValidationProblem>> (
            int id, PostponeChoreRequest request, ChoresDbContext db, TimeProvider clock) =>
        {
            if (await Find(db, id) is not { } chore)
            {
                return TypedResults.NotFound();
            }
            if (!IsCurrent(chore, request.DueDate))
            {
                return TypedResults.Conflict();
            }

            Result<Chore> result = request.PostponeOn(chore, clock.Today());
            if (result.Value is not { } postponed)
            {
                return result.ToProblem();
            }

            await db.SaveChangesAsync();
            return TypedResults.Ok(ChoreDto.From(postponed, entity => IdOf(db, entity), clock));
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

    private static async Task<Area?> FindArea(ChoresDbContext db, int? id) => id is { } areaId ? await db.Areas.FindAsync(areaId) : null;

    // Keys are shadow properties, see ChoresDbContext
    private static int IdOf(ChoresDbContext db, object entity) => (int)db.Entry(entity).Property("Id").CurrentValue!;
}
