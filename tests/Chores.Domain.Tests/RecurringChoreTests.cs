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
        return chore.DueDate;
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
        Assert.Equal(CreatedOn, NewChore(7, IntervalUnit.Day).DueDate);
    }

    [Fact]
    public void Without_a_Completion_it_is_due_on_the_first_Preferred_Day_from_the_creation_date()
    {
        // 1 January 2027 is a Friday
        Assert.Equal(new DateOnly(2027, 1, 5), NewChore(7, IntervalUnit.Day, DayOfWeek.Tuesday).DueDate);
        Assert.Equal(CreatedOn, NewChore(7, IntervalUnit.Day, DayOfWeek.Friday).DueDate);
    }

    [Fact]
    public void Last_Done_is_the_latest_Completion_even_when_recorded_out_of_order()
    {
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Complete(new DateOnly(2027, 3, 10));
        chore.Complete(new DateOnly(2027, 3, 3));

        Assert.Equal(new DateOnly(2027, 3, 10), chore.LastDone);
        Assert.Equal(new DateOnly(2027, 3, 17), chore.DueDate);
    }

    [Fact]
    public void Removing_a_Completion_falls_back_to_the_previous_one()
    {
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Complete(new DateOnly(2027, 3, 3));
        chore.Complete(new DateOnly(2027, 3, 10));

        chore.RemoveCompletion(chore.Completions.Single(c => c.Date == new DateOnly(2027, 3, 10)));

        Assert.Equal(new DateOnly(2027, 3, 3), chore.LastDone);
        Assert.Equal(new DateOnly(2027, 3, 10), chore.DueDate);
    }

    [Fact]
    public void Postponed_it_is_due_on_the_Postpone_date_until_the_next_Completion()
    {
        var today = new DateOnly(2027, 3, 10);
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Complete(new DateOnly(2027, 3, 3));

        chore.Postpone(new DateOnly(2027, 3, 13));
        Assert.Equal(new DateOnly(2027, 3, 13), chore.DueDate);

        chore.Complete(today);
        Assert.Equal(new DateOnly(2027, 3, 17), chore.DueDate);
    }

    [Fact]
    public void Back_dated_Completion_before_Last_Done_keeps_the_Postpone()
    {
        var today = new DateOnly(2027, 3, 10);
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Complete(new DateOnly(2027, 3, 3));
        chore.Postpone(new DateOnly(2027, 3, 13));

        chore.Complete(new DateOnly(2027, 2, 20));

        Assert.Equal(new DateOnly(2027, 3, 13), chore.DueDate);
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

        Assert.Equal(new DateOnly(2027, 3, 13), chore.DueDate);
    }

    [Fact]
    public void Normal_7_day_Chore_last_done_10_days_ago_is_Overdue_with_Urgency_1_75()
    {
        var today = new DateOnly(2027, 3, 11);
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Complete(today.AddDays(-10));

        Assert.True(chore.IsOverdue(new FixedClock(today)));
        Assert.Equal(1.75, chore.Urgency(new FixedClock(today)));
    }

    [Fact]
    public void Low_Chore_two_days_Overdue_has_Urgency_1_125()
    {
        var today = new DateOnly(2027, 3, 11);
        RecurringChore chore = NewChore(14, IntervalUnit.Day);
        chore.Importance = Importance.Low;
        chore.Complete(today.AddDays(-16));

        Assert.Equal(1.125, chore.Urgency(new FixedClock(today)));
    }

    [Fact]
    public void High_Chore_due_today_has_Urgency_1_5_and_is_not_Overdue()
    {
        var today = new DateOnly(2027, 3, 11);
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Importance = Importance.High;
        chore.Complete(today.AddDays(-7));

        Assert.False(chore.IsOverdue(new FixedClock(today)));
        Assert.Equal(1.5, chore.Urgency(new FixedClock(today)));
    }

    [Fact]
    public void High_yearly_Chore_four_months_before_its_Due_Date_has_no_Urgency_yet()
    {
        RecurringChore chore = NewChore(1, IntervalUnit.Year);
        chore.Importance = Importance.High;
        chore.Complete(new DateOnly(2026, 3, 1));

        Assert.Equal(0, chore.Urgency(new FixedClock(new DateOnly(2026, 11, 1))));
    }

    [Theory]
    [InlineData(-10, 0)]
    [InlineData(-4, 0)]
    [InlineData(-2, 0.5)]
    [InlineData(0, 1)]
    [InlineData(2, 1.5)]
    [InlineData(4, 2)]
    [InlineData(10, 2)]
    public void Urgency_rises_from_0_four_days_before_the_Due_Date_to_2_four_days_after(int daysAfterDueDate, double urgency)
    {
        var dueDate = new DateOnly(2027, 3, 11);
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Complete(dueDate.AddDays(-7));

        Assert.Equal(urgency, chore.Urgency(new FixedClock(dueDate.AddDays(daysAfterDueDate))));
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
    public void Season_affects_only_the_Due_Date_not_Overdue_or_Urgency()
    {
        var june = new DateOnly(2027, 6, 15);
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Season = new Season(4, 5);
        chore.Complete(new DateOnly(2027, 5, 16));

        // Due 23 May, still inside the Season, and not done since
        Assert.Equal(new DateOnly(2027, 5, 23), chore.DueDate);
        Assert.True(chore.IsDormant(new FixedClock(june)));
        Assert.True(chore.IsOverdue(new FixedClock(june)));
        Assert.Equal(2, chore.Urgency(new FixedClock(june)));
    }

    [Fact]
    public void Chore_without_a_Season_is_never_Dormant()
    {
        Assert.False(NewChore(7, IntervalUnit.Day).IsDormant(new FixedClock(new DateOnly(2027, 6, 15))));
    }

    [Fact]
    public void Due_Date_outside_the_Season_follows_the_schedule_to_the_first_one_inside_it()
    {
        RecurringChore mowing = NewChore(2, IntervalUnit.Week, DayOfWeek.Saturday);
        mowing.Season = new Season(4, 10);
        mowing.Complete(new DateOnly(2026, 10, 23));

        // Saturday 7 November 2026 plus whole fortnights
        Assert.Equal(new DateOnly(2027, 4, 10), mowing.DueDate);
        Assert.False(mowing.IsOverdue(new FixedClock(new DateOnly(2027, 4, 1))));
    }

    [Fact]
    public void Due_Date_outside_the_Season_without_a_Preferred_Day_follows_the_schedule()
    {
        RecurringChore mowing = NewChore(2, IntervalUnit.Week);
        mowing.Season = new Season(4, 10);
        mowing.Complete(new DateOnly(2026, 10, 23));

        // Friday 6 November 2026 plus whole fortnights
        Assert.Equal(new DateOnly(2027, 4, 9), mowing.DueDate);
    }

    [Fact]
    public void Schedule_that_never_lands_inside_the_Season_is_due_on_the_Season_start()
    {
        RecurringChore chore = NewChore(1, IntervalUnit.Year);
        chore.Season = new Season(4, 10);
        chore.Complete(new DateOnly(2026, 3, 15));

        Assert.Equal(new DateOnly(2027, 4, 1), chore.DueDate);
    }

    [Fact]
    public void Created_while_Dormant_it_is_due_on_the_first_day_of_the_Season()
    {
        var mowing = new RecurringChore("Mow the lawn", new Interval(2, IntervalUnit.Week), new DateOnly(2026, 12, 1))
        {
            Season = new Season(4, 10),
        };

        Assert.Equal(new DateOnly(2027, 4, 1), mowing.DueDate);
    }

    [Fact]
    public void Due_Date_after_a_Season_that_wraps_the_year_follows_the_schedule_into_the_next_one()
    {
        RecurringChore chore = NewChore(1, IntervalUnit.Week);
        chore.Season = new Season(11, 2);
        chore.Complete(new DateOnly(2027, 2, 27));

        // Saturday 6 March is outside; the first weekly Saturday from Monday 1 November
        Assert.Equal(new DateOnly(2027, 11, 6), chore.DueDate);
    }

    [Fact]
    public void Due_Date_inside_the_Season_stays_as_it_is()
    {
        RecurringChore chore = NewChore(1, IntervalUnit.Year);
        chore.Season = new Season(4, 10);
        chore.Complete(new DateOnly(2026, 5, 15));

        Assert.Equal(new DateOnly(2027, 5, 15), chore.DueDate);
    }

    [Fact]
    public void Interval_must_be_at_least_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Interval(0, IntervalUnit.Day));
    }
}
