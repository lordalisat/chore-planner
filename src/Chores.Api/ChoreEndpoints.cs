using Chores.Api.Data;
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
            // The Domain decides what is valid; each rejection is reported at the field the form shows it on
            var errors = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                errors["name"] = ["A Chore needs a name."];
            }
            // Missing when the JSON leaves it out, whatever its type says
            Interval? interval = OrFieldError("interval", "Every needs a whole number of at least 1.", () => request.Interval is { } given
                ? new Interval(given.Count, given.Unit)
                : throw new ArgumentNullException(nameof(request.Interval)));
            PreferredDay? preferredDay = null;
            if (request.PreferredDay is not null && !PreferredDayText.TryFromText(request.PreferredDay, out preferredDay))
            {
                errors["preferredDay"] = ["That is not a day."];
            }
            else if (interval is not null && preferredDay?.Allows(interval) == false)
            {
                errors["preferredDay"] = ["The first or last of the month needs an Interval in months or years."];
            }
            Season? season = request.Season is null
                ? null
                : OrFieldError("season", "A Season runs between months 1 and 12.", () => new Season(request.Season.FromMonth, request.Season.ToMonth));
            if (errors.Count > 0)
            {
                return TypedResults.ValidationProblem(errors);
            }

            var chore = new RecurringChore(request.Name.Trim(), interval!, clock.Today())
            {
                PreferredDay = preferredDay,
                Season = season,
                Importance = request.Importance,
                Effort = request.Effort,
            };
            // Moving over from elsewhere: the Due Date follows from when it was last done, not from today
            if (request.LastDone is { } lastDone)
            {
                chore.Complete(lastDone);
            }
            db.Add(chore);
            await db.SaveChangesAsync();
            // No address yet: there is no endpoint for one Chore
            return TypedResults.Created((string?)null, ChoreDto.From(chore, entity => IdOf(db, entity), clock));

            T? OrFieldError<T>(string field, string error, Func<T> build) where T : class
            {
                try
                {
                    return build();
                }
                catch (ArgumentException)
                {
                    errors[field] = [error];
                    return null;
                }
            }
        });
    }

    // Keys are shadow properties, see ChoresDbContext
    private static int IdOf(ChoresDbContext db, object entity) => (int)db.Entry(entity).Property("Id").CurrentValue!;

    // Defaults apply to properties the JSON leaves out
    private sealed record CreateRecurringChoreRequest(
        string Name,
        IntervalDto Interval,
        string? PreferredDay = null,
        SeasonDto? Season = null,
        Importance Importance = Importance.Normal,
        int? Effort = null,
        DateOnly? LastDone = null);
}

/// <summary>A Chore as the front end sees it, with its scheduling computed for today.</summary>
/// <param name="Kind">"Recurring" or "OneOff".</param>
/// <param name="PreferredDay">"Monday" to "Sunday", "FirstOfMonth" or "LastOfMonth".</param>
/// <param name="Urgency">How pressing the Chore is today; null without a Due Date.</param>
/// <param name="Dormant">Outside its Season; the Overview hides it.</param>
public sealed record ChoreDto(
    int Id,
    string Kind,
    string Name,
    Importance Importance,
    int? Effort,
    AreaRefDto? Area,
    IntervalDto? Interval,
    string? PreferredDay,
    SeasonDto? Season,
    DateOnly? DueDate,
    double? Urgency,
    bool Overdue,
    bool Dormant,
    DateOnly? LastDone)
{
    /// <param name="idOf">The key of a Chore or Area, which the Domain does not hold.</param>
    public static ChoreDto From(Chore chore, Func<object, int> idOf, TimeProvider clock)
    {
        var recurring = chore as RecurringChore;
        return new ChoreDto(
            idOf(chore),
            recurring is null ? "OneOff" : "Recurring",
            chore.Name,
            chore.Importance,
            chore.Effort,
            chore.Area is null ? null : new AreaRefDto(idOf(chore.Area), chore.Area.Name),
            recurring is null ? null : new IntervalDto(recurring.Interval.Count, recurring.Interval.Unit),
            recurring?.PreferredDay is { } day ? PreferredDayText.ToText(day) : null,
            recurring?.Season is { } season ? new SeasonDto(season.FromMonth, season.ToMonth) : null,
            recurring?.DueDate ?? (chore as OneOffChore)?.DueDate,
            chore.Urgency(clock),
            chore.IsOverdue(clock),
            recurring?.IsDormant(clock) ?? false,
            chore.LastDone);
    }
}

public sealed record IntervalDto(int Count, IntervalUnit Unit);

/// <param name="FromMonth">1 to 12, January to December.</param>
/// <param name="ToMonth">1 to 12; before FromMonth when the Season wraps the year.</param>
public sealed record SeasonDto(int FromMonth, int ToMonth);

public sealed record AreaRefDto(int Id, string Name);
