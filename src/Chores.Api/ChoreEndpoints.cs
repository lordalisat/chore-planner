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
            return TypedResults.Ok(all.Select(chore => ChoreDto.From(chore, db.IdOf, clock)).ToList());
        });

        chores.MapGet("/{id:int}", async Task<Results<Ok<ChoreDto>, NotFound>> (int id, ChoresDbContext db, TimeProvider clock) =>
            await Find(db, id) is { } chore
                ? TypedResults.Ok(ChoreDto.From(chore, db.IdOf, clock))
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
            var created = ChoreDto.From(chore, db.IdOf, clock);
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
            Result<Chore> result = request.ApplyTo(chore, takenNames, await FindArea(db, request.AreaId), db.IdOf);
            if (result.Value is not { } edited)
            {
                return result.ToProblem();
            }

            await db.SaveChangesAsync();
            return TypedResults.Ok(ChoreDto.From(edited, db.IdOf, clock));
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
            var created = ChoreDto.From(chore, db.IdOf, clock);
            return TypedResults.Created($"/api/chores/{created.Id}", created);
        });

        // Keeps the history of a Chore that has any; an Archived Chore is not found, so it is never removed for good
        chores.MapDelete("/{id:int}", async Task<Results<Ok<DeleteChoreResponse>, NotFound>> (int id, ChoresDbContext db, TimeProvider clock) =>
        {
            if (await db.Chores.SingleOrDefaultAsync(c => EF.Property<int>(c, "Id") == id) is not { } chore)
            {
                return TypedResults.NotFound();
            }

            bool archive = chore.HasHistory;
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

        // Not while an active Chore has taken its Name since
        chores.MapPost("/{id:int}/restore", async Task<Results<Ok<ChoreDto>, NotFound, ValidationProblem>> (
            int id, ChoresDbContext db, TimeProvider clock) =>
        {
            if (await db.Chores.IgnoreQueryFilters().Include(c => c.Area)
                    .SingleOrDefaultAsync(c => EF.Property<int>(c, "Id") == id && c.ArchivedOn != null) is not { } chore)
            {
                return TypedResults.NotFound();
            }
            List<FieldError> errors = [];
            errors.CheckName(chore.Name, await db.Chores.Select(c => c.Name).ToListAsync());
            Result<Chore> result = new(errors, () =>
            {
                chore.Restore();
                return chore;
            });
            if (result.Value is not { } restored)
            {
                return result.ToProblem();
            }

            await db.SaveChangesAsync();
            return TypedResults.Ok(ChoreDto.From(restored, db.IdOf, clock));
        });

        chores.MapPost("/{id:int}/complete", (int id, CompleteChoreRequest request, ChoresDbContext db, TimeProvider clock) =>
            ActOn(db, id, request.DueDate, chore => request.CompleteOn(chore, clock.Today()), chore => Recorded(db, chore, clock)));

        chores.MapPost("/{id:int}/skip", (int id, SkipChoreRequest request, ChoresDbContext db, TimeProvider clock) =>
            ActOn(db, id, request.DueDate, chore => request.SkipOn(chore, clock.Today()), chore => Recorded(db, chore, clock)));

        chores.MapPost("/{id:int}/postpone", (int id, PostponeChoreRequest request, ChoresDbContext db, TimeProvider clock) =>
            ActOn(db, id, request.DueDate, chore => request.PostponeOn(chore, clock.Today()), chore => ChoreDto.From(chore, db.IdOf, clock)));

        // Ticking records no Completion, so it needs no Due Date check: any device may tick or untick any time
        chores.MapPut("/{id:int}/items/{itemId:int}", async Task<Results<Ok<ChoreDto>, NotFound>> (
            int id, int itemId, TickItemRequest request, ChoresDbContext db, TimeProvider clock) =>
        {
            if (await Find(db, id) is not { } chore || chore.Items.FirstOrDefault(i => db.IdOf(i) == itemId) is not { } item)
            {
                return TypedResults.NotFound();
            }

            if (request.Ticked)
            {
                item.Tick();
            }
            else
            {
                item.Untick();
            }
            await db.SaveChangesAsync();
            return TypedResults.Ok(ChoreDto.From(chore, db.IdOf, clock));
        });

        // Undo for a Completion, done or skipped
        chores.MapDelete("/{id:int}/completions/{completionId:int}", async Task<Results<Ok<ChoreDto>, NotFound>> (
            int id, int completionId, ChoresDbContext db, TimeProvider clock) =>
        {
            if (await Find(db, id) is not { } chore
                || chore.Completions.FirstOrDefault(c => db.IdOf(c) == completionId) is not { } completion)
            {
                return TypedResults.NotFound();
            }

            chore.RemoveCompletion(completion);
            await db.SaveChangesAsync();
            return TypedResults.Ok(ChoreDto.From(chore, db.IdOf, clock));
        });
    }

    private static Task<Chore?> Find(ChoresDbContext db, int id) =>
        db.Chores.Include(c => c.Area).SingleOrDefaultAsync(c => EF.Property<int>(c, "Id") == id);

    private static async Task<Area?> FindArea(ChoresDbContext db, int? id) => id is { } areaId ? await db.Areas.FindAsync(areaId) : null;

    /// <summary>
    /// Acts on a Chore only while it still has the Due Date the device saw, so two devices can't both act on it:
    /// a conflict when someone else acted first, or what was wrong with the request.
    /// </summary>
    private static async Task<Results<Ok<T>, NotFound, Conflict, ValidationProblem>> ActOn<T>(
        ChoresDbContext db, int id, DateOnly? seenDueDate, Func<Chore, Result<Chore>> act, Func<Chore, T> answer)
    {
        if (await Find(db, id) is not { } chore)
        {
            return TypedResults.NotFound();
        }
        if (!chore.IsDueOn(seenDueDate))
        {
            return TypedResults.Conflict();
        }

        Result<Chore> result = act(chore);
        if (result.Value is not { } acted)
        {
            return result.ToProblem();
        }

        await db.SaveChangesAsync();
        return TypedResults.Ok(answer(acted));
    }

    // The Completion just recorded, which Undo removes
    private static RecordedCompletionDto Recorded(ChoresDbContext db, Chore chore, TimeProvider clock) =>
        new(db.IdOf(chore.Completions[^1]), ChoreDto.From(chore, db.IdOf, clock));
}
