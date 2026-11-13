using Xunit;

namespace Chores.Domain.Tests;

public class OneOffChoreTests
{
    private static readonly DateOnly CreatedOn = new(2027, 3, 1);
    private static readonly FixedClock Today = new(new DateOnly(2027, 3, 11));

    [Fact]
    public void Due_Date_is_optional_and_set_by_hand()
    {
        var chore = new OneOffChore("Fix the leaking tap", CreatedOn);
        Assert.Null(chore.DueDate(Today));

        chore.DueOn = new DateOnly(2027, 3, 20);
        Assert.Equal(new DateOnly(2027, 3, 20), chore.DueDate(Today));
    }

    [Fact]
    public void Postpone_changes_the_Due_Date()
    {
        var chore = new OneOffChore("Fix the leaking tap", CreatedOn) { DueOn = new DateOnly(2027, 3, 5) };

        chore.Postpone(new DateOnly(2027, 3, 21));

        Assert.Equal(new DateOnly(2027, 3, 21), chore.DueDate(Today));
        Assert.False(chore.IsOverdue(Today));
    }

    [Fact]
    public void Overdue_when_today_is_after_the_Due_Date()
    {
        var chore = new OneOffChore("Fix the leaking tap", CreatedOn) { DueOn = new DateOnly(2027, 3, 10) };

        Assert.True(chore.IsOverdue(Today));
    }

    [Fact]
    public void Without_a_Due_Date_it_is_never_Overdue_and_progress_is_one_half()
    {
        var chore = new OneOffChore("Fix the leaking tap", CreatedOn) { Importance = Importance.High };

        Assert.False(chore.IsOverdue(Today));
        Assert.Equal(0.75, chore.Urgency(Today));
    }

    [Fact]
    public void With_a_Due_Date_progress_runs_from_the_creation_date()
    {
        var chore = new OneOffChore("Fix the leaking tap", CreatedOn) { DueOn = new DateOnly(2027, 3, 21) };

        Assert.Equal(0.5, chore.Urgency(Today));
    }

    [Fact]
    public void Completed_it_is_finished_and_has_no_Urgency()
    {
        var chore = new OneOffChore("Fix the leaking tap", CreatedOn) { DueOn = new DateOnly(2027, 3, 5) };

        chore.Complete(new DateOnly(2027, 3, 9));

        Assert.True(chore.IsFinished);
        Assert.Null(chore.Urgency(Today));
        Assert.False(chore.IsOverdue(Today));
    }
}
