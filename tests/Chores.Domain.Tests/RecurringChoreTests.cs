using Xunit;

namespace Chores.Domain.Tests;

public class RecurringChoreTests
{
    private static readonly DateOnly CreatedOn = new(2027, 1, 1);

    private static RecurringChore NewChore(int count, IntervalUnit unit, DayOfWeek? preferredDay = null) =>
        new("Laundry", new Interval(count, unit), CreatedOn) { PreferredDay = preferredDay };

    private static DateOnly? DueDateAfterDoneOn(RecurringChore chore, DateOnly lastDone)
    {
        chore.Complete(lastDone);
        return chore.DueDate(new FixedClock(lastDone));
    }

    [Fact]
    public void Laundry_done_on_a_Thursday_is_due_the_Tuesday_five_days_later()
    {
        var thursday = new DateOnly(2027, 3, 11);

        Assert.Equal(new DateOnly(2027, 3, 16), DueDateAfterDoneOn(NewChore(7, IntervalUnit.Day, DayOfWeek.Tuesday), thursday));
    }

    [Fact]
    public void Laundry_done_on_a_Monday_is_due_the_Tuesday_eight_days_later()
    {
        var monday = new DateOnly(2027, 3, 8);

        Assert.Equal(new DateOnly(2027, 3, 16), DueDateAfterDoneOn(NewChore(7, IntervalUnit.Day, DayOfWeek.Tuesday), monday));
    }

    [Fact]
    public void Preferred_Day_on_or_before_Last_Done_moves_to_the_following_one()
    {
        var wednesday = new DateOnly(2027, 3, 10);

        // Thursday's nearest Tuesday is the day before Last Done
        Assert.Equal(new DateOnly(2027, 3, 16), DueDateAfterDoneOn(NewChore(1, IntervalUnit.Day, DayOfWeek.Tuesday), wednesday));
    }

    [Fact]
    public void Every_month_done_31_January_is_due_28_February()
    {
        Assert.Equal(new DateOnly(2027, 2, 28), DueDateAfterDoneOn(NewChore(1, IntervalUnit.Month), new DateOnly(2027, 1, 31)));
    }

    [Fact]
    public void Every_year_done_29_February_is_due_28_February_next_year()
    {
        Assert.Equal(new DateOnly(2029, 2, 28), DueDateAfterDoneOn(NewChore(1, IntervalUnit.Year), new DateOnly(2028, 2, 29)));
    }

    [Fact]
    public void Every_month_on_Saturday_done_15_March_is_due_Saturday_17_April()
    {
        Assert.Equal(new DateOnly(2027, 4, 17), DueDateAfterDoneOn(NewChore(1, IntervalUnit.Month, DayOfWeek.Saturday), new DateOnly(2027, 3, 15)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(DayOfWeek.Friday)]
    public void Every_2_weeks_behaves_exactly_like_every_14_days(DayOfWeek? preferredDay)
    {
        var lastDone = new DateOnly(2027, 3, 8);

        Assert.Equal(
            DueDateAfterDoneOn(NewChore(14, IntervalUnit.Day, preferredDay), lastDone),
            DueDateAfterDoneOn(NewChore(2, IntervalUnit.Week, preferredDay), lastDone));
    }

    [Fact]
    public void Without_a_Completion_it_is_due_on_the_creation_date()
    {
        Assert.Equal(CreatedOn, NewChore(7, IntervalUnit.Day).DueDate(new FixedClock(CreatedOn)));
    }

    [Fact]
    public void Without_a_Completion_it_is_due_on_the_first_Preferred_Day_from_the_creation_date()
    {
        // 1 January 2027 is a Friday
        Assert.Equal(new DateOnly(2027, 1, 5), NewChore(7, IntervalUnit.Day, DayOfWeek.Tuesday).DueDate(new FixedClock(CreatedOn)));
        Assert.Equal(CreatedOn, NewChore(7, IntervalUnit.Day, DayOfWeek.Friday).DueDate(new FixedClock(CreatedOn)));
    }

    [Fact]
    public void Last_Done_is_the_latest_Completion_even_when_recorded_out_of_order()
    {
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Complete(new DateOnly(2027, 3, 10));
        chore.Complete(new DateOnly(2027, 3, 3));

        Assert.Equal(new DateOnly(2027, 3, 10), chore.LastDone);
        Assert.Equal(new DateOnly(2027, 3, 17), chore.DueDate(new FixedClock(new DateOnly(2027, 3, 10))));
    }

    [Fact]
    public void Removing_a_Completion_falls_back_to_the_previous_one()
    {
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Complete(new DateOnly(2027, 3, 3));
        chore.Complete(new DateOnly(2027, 3, 10));

        chore.RemoveCompletion(chore.Completions.Single(c => c.Date == new DateOnly(2027, 3, 10)));

        Assert.Equal(new DateOnly(2027, 3, 3), chore.LastDone);
        Assert.Equal(new DateOnly(2027, 3, 10), chore.DueDate(new FixedClock(new DateOnly(2027, 3, 10))));
    }

    [Fact]
    public void Postponed_it_is_due_on_the_Postpone_date_until_the_next_Completion()
    {
        var today = new DateOnly(2027, 3, 10);
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Complete(new DateOnly(2027, 3, 3));

        chore.Postpone(new DateOnly(2027, 3, 13));
        Assert.Equal(new DateOnly(2027, 3, 13), chore.DueDate(new FixedClock(today)));

        chore.Complete(today);
        Assert.Equal(new DateOnly(2027, 3, 17), chore.DueDate(new FixedClock(today)));
    }

    [Fact]
    public void Back_dated_Completion_before_Last_Done_keeps_the_Postpone()
    {
        var today = new DateOnly(2027, 3, 10);
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Complete(new DateOnly(2027, 3, 3));
        chore.Postpone(new DateOnly(2027, 3, 13));

        chore.Complete(new DateOnly(2027, 2, 20));

        Assert.Equal(new DateOnly(2027, 3, 13), chore.DueDate(new FixedClock(today)));
    }

    [Fact]
    public void Removing_the_Completion_after_a_Postpone_restores_the_Postpone()
    {
        var today = new DateOnly(2027, 3, 10);
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Complete(new DateOnly(2027, 3, 3));
        chore.Postpone(new DateOnly(2027, 3, 13));
        chore.Complete(today);

        chore.RemoveCompletion(chore.Completions.Single(c => c.Date == today));

        Assert.Equal(new DateOnly(2027, 3, 13), chore.DueDate(new FixedClock(today)));
    }

    [Fact]
    public void Normal_7_day_Chore_last_done_10_days_ago_is_Overdue_with_Urgency_1_43()
    {
        var today = new DateOnly(2027, 3, 11);
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Complete(today.AddDays(-10));

        Assert.True(chore.IsOverdue(new FixedClock(today)));
        Assert.Equal(1.43, chore.Urgency(new FixedClock(today))!.Value, 0.01);
    }

    [Fact]
    public void Low_14_day_Chore_two_days_Overdue_has_Urgency_0_86()
    {
        var today = new DateOnly(2027, 3, 11);
        RecurringChore chore = NewChore(14, IntervalUnit.Day);
        chore.Importance = Importance.Low;
        chore.Complete(today.AddDays(-16));

        Assert.Equal(0.86, chore.Urgency(new FixedClock(today))!.Value, 0.01);
    }

    [Fact]
    public void High_Chore_due_today_has_Urgency_1_5_and_is_not_Overdue()
    {
        var today = new DateOnly(2027, 3, 11);
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Importance = Importance.High;
        chore.Complete(today.AddDays(-7));

        Assert.False(chore.IsOverdue(new FixedClock(today)));
        Assert.Equal(1.5, chore.Urgency(new FixedClock(today))!.Value, 0.01);
    }

    [Fact]
    public void Importance_plays_no_part_in_Overdue()
    {
        var today = new DateOnly(2027, 3, 11);
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Importance = Importance.Low;
        chore.Complete(today.AddDays(-8));

        Assert.True(chore.IsOverdue(new FixedClock(today)));
    }

    [Fact]
    public void Without_a_Completion_progress_runs_from_the_creation_date()
    {
        // Due on the creation date itself: progress is 1
        Assert.Equal(1, NewChore(7, IntervalUnit.Day).Urgency(new FixedClock(CreatedOn)));

        // Created 1 January, postponed to 9 January, two days in
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Postpone(new DateOnly(2027, 1, 9));
        Assert.Equal(2.0 / 8, chore.Urgency(new FixedClock(new DateOnly(2027, 1, 3))));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(6, true)]
    [InlineData(11, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public void Season_from_November_to_February_wraps_the_year(int month, bool dormant)
    {
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Season = new Season(11, 2);

        Assert.Equal(dormant, chore.IsDormant(new FixedClock(new DateOnly(2027, month, 15))));
    }

    [Fact]
    public void Dormant_Chore_has_no_Due_Date_is_never_Overdue_and_has_no_Urgency()
    {
        var june = new DateOnly(2027, 6, 15);
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Season = new Season(4, 5);
        chore.Complete(june.AddDays(-30));

        Assert.True(chore.IsDormant(new FixedClock(june)));
        Assert.Null(chore.DueDate(new FixedClock(june)));
        Assert.False(chore.IsOverdue(new FixedClock(june)));
        Assert.Null(chore.Urgency(new FixedClock(june)));
    }

    [Fact]
    public void Chore_without_a_Season_is_never_Dormant()
    {
        Assert.False(NewChore(7, IntervalUnit.Day).IsDormant(new FixedClock(new DateOnly(2027, 6, 15))));
    }

    [Fact]
    public void Interval_must_be_at_least_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Interval(0, IntervalUnit.Day));
    }
}
