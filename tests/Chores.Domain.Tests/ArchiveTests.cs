using Xunit;

namespace Chores.Domain.Tests;

public class ArchiveTests
{
    private static readonly DateOnly CreatedOn = new(2027, 1, 1);
    private static readonly FixedClock Today = new(new DateOnly(2027, 3, 11));

    [Fact]
    public void An_Archived_Chore_keeps_its_Completions_and_the_date_it_was_Archived_on()
    {
        var laundry = new RecurringChore("Laundry", new Interval(1, IntervalUnit.Week), CreatedOn);
        laundry.Complete(new DateOnly(2027, 2, 1));
        Assert.Null(laundry.ArchivedOn);

        laundry.Archive(new DateOnly(2027, 3, 11));

        Assert.Equal(new DateOnly(2027, 3, 11), laundry.ArchivedOn);
        Assert.Single(laundry.Completions);
    }

    [Fact]
    public void An_Archived_Chore_is_never_Overdue_and_has_no_Urgency()
    {
        // Due 8 February, so Overdue today until Archived
        var laundry = new RecurringChore("Laundry", new Interval(1, IntervalUnit.Week), CreatedOn);
        laundry.Complete(new DateOnly(2027, 2, 1));
        var tap = new OneOffChore("Fix the leaking tap", CreatedOn) { DueDate = new DateOnly(2027, 3, 1) };

        laundry.Archive(Today.Today());
        tap.Archive(Today.Today());

        Assert.False(laundry.IsOverdue(Today));
        Assert.Null(laundry.Urgency(Today));
        Assert.False(tap.IsOverdue(Today));
        Assert.Null(tap.Urgency(Today));
    }

    [Fact]
    public void A_Restored_Chore_is_scheduled_again_from_its_history()
    {
        var laundry = new RecurringChore("Laundry", new Interval(1, IntervalUnit.Week), CreatedOn);
        laundry.Complete(new DateOnly(2027, 2, 1));
        laundry.Archive(new DateOnly(2027, 3, 1));

        laundry.Restore();

        Assert.Null(laundry.ArchivedOn);
        Assert.True(laundry.IsOverdue(Today));
        Assert.Equal(new DateOnly(2027, 2, 8), laundry.DueDate);
        Assert.Equal(new DateOnly(2027, 2, 1), laundry.LastDone);
    }
}
