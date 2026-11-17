using Chores.Api.Data;
using Chores.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Chores.Api.Tests;

/// <summary>Saves through one context and loads through a fresh one, against a migrated in-memory SQLite database.</summary>
public sealed class PersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public PersistenceTests()
    {
        _connection.Open();
        using ChoresDbContext db = NewContext();
        db.Database.Migrate();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public void An_Area_keeps_its_parent_Area()
    {
        Save(new Area("Kitchen", new Area("Downstairs")));

        using ChoresDbContext db = NewContext();
        Area kitchen = db.Areas.Include(a => a.Parent).Single(a => a.Name == "Kitchen");

        Assert.Equal("Downstairs", kitchen.Parent?.Name);
    }

    [Fact]
    public void A_Recurring_Chore_keeps_its_schedule()
    {
        var laundry = new RecurringChore("Laundry", new Interval(7, IntervalUnit.Day), new DateOnly(2027, 1, 1))
        {
            PreferredDay = DayOfWeek.Tuesday,
            Importance = Importance.High,
            Effort = 30,
            Area = new Area("Utility room"),
        };
        laundry.Complete(new DateOnly(2027, 3, 4)); // a Thursday
        Save(laundry);

        RecurringChore loaded = LoadChore<RecurringChore>("Laundry");

        Assert.Equal(new DateOnly(2027, 3, 9), loaded.DueDate);
        Assert.Equal(new Interval(7, IntervalUnit.Day), loaded.Interval);
        Assert.Equal(new DateOnly(2027, 1, 1), loaded.CreatedOn);
        Assert.Equal(Importance.High, loaded.Importance);
        Assert.Equal(30, loaded.Effort);
        Assert.Equal("Utility room", loaded.Area?.Name);
    }

    [Fact]
    public void A_Recurring_Chore_keeps_its_Completions_and_Postpone()
    {
        var vacuuming = new RecurringChore("Vacuuming", new Interval(7, IntervalUnit.Day), new DateOnly(2027, 1, 1));
        vacuuming.Complete(new DateOnly(2027, 1, 5));
        vacuuming.Skip(new DateOnly(2027, 1, 10));
        vacuuming.Postpone(new DateOnly(2027, 1, 21));
        Save(vacuuming);

        RecurringChore loaded = LoadChore<RecurringChore>("Vacuuming");

        Assert.Equal(
            [
                new Completion(new DateOnly(2027, 1, 5), new DateOnly(2027, 1, 1), null, Skipped: false),
                new Completion(new DateOnly(2027, 1, 10), new DateOnly(2027, 1, 12), null, Skipped: true),
            ],
            loaded.Completions);
        Assert.Equal(new DateOnly(2027, 1, 5), loaded.LastDone);
        Assert.Equal(new DateOnly(2027, 1, 21), loaded.PostponedTo);
        Assert.Equal(new DateOnly(2027, 1, 21), loaded.DueDate);
    }

    [Fact]
    public void A_Recurring_Chore_keeps_its_Season()
    {
        Save(new RecurringChore("Clear the gutters", new Interval(1, IntervalUnit.Month), new DateOnly(2027, 1, 1))
        {
            Season = new Season(11, 2),
        });

        Assert.Equal(new Season(11, 2), LoadChore<RecurringChore>("Clear the gutters").Season);
    }

    [Fact]
    public void A_finished_One_off_Chore_keeps_its_Due_Date()
    {
        var tap = new OneOffChore("Fix the leaking tap", new DateOnly(2027, 1, 1)) { DueDate = new DateOnly(2027, 1, 15) };
        tap.Complete(new DateOnly(2027, 1, 12));
        Save(tap);

        OneOffChore loaded = LoadChore<OneOffChore>("Fix the leaking tap");

        Assert.True(loaded.IsFinished);
        Assert.Equal(new DateOnly(2027, 1, 15), loaded.DueDate);
    }

    private T LoadChore<T>(string name) where T : Chore
    {
        using ChoresDbContext db = NewContext();
        return db.Chores.OfType<T>().Include(c => c.Area).Single(c => c.Name == name);
    }

    private void Save(object entity)
    {
        using ChoresDbContext db = NewContext();
        db.Add(entity);
        db.SaveChanges();
    }

    private ChoresDbContext NewContext() => new(new DbContextOptionsBuilder<ChoresDbContext>().UseSqlite(_connection).Options);
}
