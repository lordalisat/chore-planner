using Xunit;

namespace Chores.Domain.Tests;

public class RecurringChoreTests
{
    private static readonly DateOnly CreatedOn = new(2027, 1, 1);

    private static RecurringChore NewChore(int count, IntervalUnit unit, PreferredDay? preferredDay = null) =>
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

        Assert.Equal(new DateOnly(2027, 3, 16), DueDateAfterDoneOn(NewChore(1, IntervalUnit.Week, DayOfWeek.Tuesday), thursday));
    }

    [Fact]
    public void Laundry_done_on_a_Monday_is_due_the_Tuesday_eight_days_later()
    {
        var monday = new DateOnly(2027, 3, 8);

        Assert.Equal(new DateOnly(2027, 3, 16), DueDateAfterDoneOn(NewChore(1, IntervalUnit.Week, DayOfWeek.Tuesday), monday));
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

    [Fact]
    public void Every_month_on_the_first_done_3_March_is_due_1_April()
    {
        Assert.Equal(new DateOnly(2027, 4, 1), DueDateAfterDoneOn(NewChore(1, IntervalUnit.Month, new PreferredDay.FirstOfMonth()), new DateOnly(2027, 3, 3)));
    }

    [Fact]
    public void Every_month_on_the_first_done_28_March_is_due_1_May()
    {
        Assert.Equal(new DateOnly(2027, 5, 1), DueDateAfterDoneOn(NewChore(1, IntervalUnit.Month, new PreferredDay.FirstOfMonth()), new DateOnly(2027, 3, 28)));
    }

    [Fact]
    public void Every_month_on_the_last_done_2_March_is_due_31_March()
    {
        Assert.Equal(new DateOnly(2027, 3, 31), DueDateAfterDoneOn(NewChore(1, IntervalUnit.Month, new PreferredDay.LastOfMonth()), new DateOnly(2027, 3, 2)));
    }

    [Fact]
    public void First_of_month_halfway_between_two_goes_to_the_later_one()
    {
        // 16 April is 15 days from both 1 April and 1 May
        Assert.Equal(new DateOnly(2027, 5, 1), DueDateAfterDoneOn(NewChore(1, IntervalUnit.Month, new PreferredDay.FirstOfMonth()), new DateOnly(2027, 3, 16)));
    }

    [Fact]
    public void Without_a_Completion_last_of_month_is_due_at_the_end_of_the_creation_month()
    {
        Assert.Equal(new DateOnly(2027, 1, 31), NewChore(3, IntervalUnit.Month, new PreferredDay.LastOfMonth()).DueDate);
    }

    [Fact]
    public void Without_a_Completion_created_on_the_first_of_month_it_is_due_that_day()
    {
        Assert.Equal(CreatedOn, NewChore(1, IntervalUnit.Year, new PreferredDay.FirstOfMonth()).DueDate);
    }

    public static TheoryData<IntervalUnit, PreferredDay> DayOrWeekWithDayInMonth => new()
    {
        { IntervalUnit.Day, new PreferredDay.FirstOfMonth() },
        { IntervalUnit.Week, new PreferredDay.LastOfMonth() },
    };

    [Theory]
    [MemberData(nameof(DayOrWeekWithDayInMonth))]
    public void First_or_last_of_month_on_a_day_or_week_Interval_is_rejected(IntervalUnit unit, PreferredDay preferredDay)
    {
        Assert.Throws<ArgumentException>(() => NewChore(1, unit, preferredDay));

        RecurringChore monthly = NewChore(1, IntervalUnit.Month, preferredDay);
        Assert.Throws<ArgumentException>(() => monthly.Interval = new Interval(1, unit));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    public void A_weekday_on_a_day_Interval_is_rejected(int days)
    {
        Assert.Throws<ArgumentException>(() => NewChore(days, IntervalUnit.Day, DayOfWeek.Monday));

        RecurringChore weekly = NewChore(1, IntervalUnit.Week, DayOfWeek.Monday);
        Assert.Throws<ArgumentException>(() => weekly.Interval = new Interval(days, IntervalUnit.Day));
    }

    [Fact]
    public void Every_2_weeks_behaves_exactly_like_every_14_days()
    {
        var lastDone = new DateOnly(2027, 3, 8);

        Assert.Equal(
            DueDateAfterDoneOn(NewChore(14, IntervalUnit.Day), lastDone),
            DueDateAfterDoneOn(NewChore(2, IntervalUnit.Week), lastDone));
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
        Assert.Equal(new DateOnly(2027, 1, 5), NewChore(1, IntervalUnit.Week, DayOfWeek.Tuesday).DueDate);
        Assert.Equal(CreatedOn, NewChore(1, IntervalUnit.Week, DayOfWeek.Friday).DueDate);
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
    public void Back_dated_Completion_ends_a_Postpone()
    {
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Complete(new DateOnly(2027, 3, 3));
        chore.Postpone(new DateOnly(2027, 3, 13));

        chore.Complete(new DateOnly(2027, 2, 20));

        Assert.Null(chore.PostponedTo);
        Assert.Equal(new DateOnly(2027, 3, 10), chore.DueDate);
    }

    [Fact]
    public void Removing_a_Completion_ends_a_Postpone()
    {
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Complete(new DateOnly(2027, 3, 3));
        chore.Complete(new DateOnly(2027, 3, 10));
        chore.Postpone(new DateOnly(2027, 3, 20));

        chore.RemoveCompletion(chore.Completions.Single(c => c.Date == new DateOnly(2027, 3, 10)));

        Assert.Null(chore.PostponedTo);
        Assert.Equal(new DateOnly(2027, 3, 10), chore.DueDate);
    }

    [Fact]
    public void Removing_a_Skip_ends_a_Postpone()
    {
        RecurringChore laundry = NewChore(1, IntervalUnit.Week, DayOfWeek.Tuesday);
        laundry.Complete(new DateOnly(2027, 3, 11));
        laundry.Skip(new DateOnly(2027, 3, 18));
        laundry.Postpone(new DateOnly(2027, 3, 26));

        laundry.RemoveCompletion(laundry.Completions.Single(c => c.Skipped));

        Assert.Null(laundry.PostponedTo);
        Assert.Equal(new DateOnly(2027, 3, 16), laundry.DueDate);
    }

    [Fact]
    public void Changing_the_Interval_reschedules_but_keeps_a_Postpone()
    {
        RecurringChore chore = NewChore(7, IntervalUnit.Day);
        chore.Complete(new DateOnly(2027, 3, 3));

        chore.Interval = new Interval(2, IntervalUnit.Week);
        Assert.Equal(new DateOnly(2027, 3, 17), chore.DueDate);

        chore.Postpone(new DateOnly(2027, 3, 20));
        chore.Interval = new Interval(1, IntervalUnit.Day);
        Assert.Equal(new DateOnly(2027, 3, 20), chore.DueDate);
    }

    [Fact]
    public void Changing_the_Preferred_Day_or_Season_reschedules()
    {
        RecurringChore chore = NewChore(1, IntervalUnit.Week);
        chore.Complete(new DateOnly(2027, 3, 11));

        chore.PreferredDay = DayOfWeek.Tuesday;
        Assert.Equal(new DateOnly(2027, 3, 16), chore.DueDate);

        chore.Season = new Season(4, 10);
        Assert.Equal(new DateOnly(2027, 4, 6), chore.DueDate);
    }

    [Fact]
    public void Completion_records_the_calculated_Due_Date_and_any_Postpone()
    {
        RecurringChore laundry = NewChore(1, IntervalUnit.Week, DayOfWeek.Tuesday);
        laundry.Complete(new DateOnly(2027, 3, 11));
        laundry.Postpone(new DateOnly(2027, 3, 18));

        laundry.Complete(new DateOnly(2027, 3, 19));

        Assert.Equal(new Completion(new DateOnly(2027, 3, 19), new DateOnly(2027, 3, 16), new DateOnly(2027, 3, 18), Skipped: false), laundry.Completions[^1]);
        Assert.Equal(new Completion(new DateOnly(2027, 3, 11), new DateOnly(2027, 1, 5), null, Skipped: false), laundry.Completions[0]);
    }

    [Fact]
    public void Skip_moves_on_from_the_skipped_Due_Date_and_leaves_Last_Done()
    {
        RecurringChore laundry = NewChore(1, IntervalUnit.Week, DayOfWeek.Tuesday);
        laundry.Complete(new DateOnly(2027, 3, 11));

        // Skipped two days after it was due, yet the next one keeps the rhythm
        laundry.Skip(new DateOnly(2027, 3, 18));
        Assert.Equal(new DateOnly(2027, 3, 23), laundry.DueDate);

        laundry.Skip(new DateOnly(2027, 3, 23));
        Assert.Equal(new DateOnly(2027, 3, 30), laundry.DueDate);

        Assert.Equal(new DateOnly(2027, 3, 11), laundry.LastDone);
        Assert.Equal(new Completion(new DateOnly(2027, 3, 18), new DateOnly(2027, 3, 16), null, Skipped: true), laundry.Completions[1]);
    }

    [Fact]
    public void Skip_without_any_Completion_moves_on_from_the_first_Due_Date()
    {
        // Created Friday 1 January, first due Tuesday 5 January
        RecurringChore laundry = NewChore(1, IntervalUnit.Week, DayOfWeek.Tuesday);

        laundry.Skip(new DateOnly(2027, 1, 5));

        Assert.Equal(new DateOnly(2027, 1, 12), laundry.DueDate);
    }

    [Fact]
    public void Removing_a_Skip_restores_the_previous_Due_Date()
    {
        RecurringChore laundry = NewChore(1, IntervalUnit.Week, DayOfWeek.Tuesday);
        laundry.Complete(new DateOnly(2027, 3, 11));
        laundry.Skip(new DateOnly(2027, 3, 18));

        laundry.RemoveCompletion(laundry.Completions.Single(c => c.Skipped));

        Assert.Equal(new DateOnly(2027, 3, 16), laundry.DueDate);
    }

    [Fact]
    public void Skip_ends_a_Postpone_and_moves_on_from_the_Postpone_date()
    {
        RecurringChore laundry = NewChore(1, IntervalUnit.Week, DayOfWeek.Tuesday);
        laundry.Complete(new DateOnly(2027, 3, 11));
        laundry.Postpone(new DateOnly(2027, 3, 20));

        laundry.Skip(new DateOnly(2027, 3, 18));

        // Saturday 20 March + 7 is Saturday 27 March; the nearest Tuesday is 30 March
        Assert.Equal(new DateOnly(2027, 3, 30), laundry.DueDate);
    }

    [Fact]
    public void Completion_after_a_Skip_moves_on_from_the_Completion()
    {
        RecurringChore laundry = NewChore(1, IntervalUnit.Week, DayOfWeek.Tuesday);
        laundry.Complete(new DateOnly(2027, 3, 11));
        laundry.Skip(new DateOnly(2027, 3, 18));

        laundry.Complete(new DateOnly(2027, 3, 25));

        Assert.Equal(new DateOnly(2027, 3, 30), laundry.DueDate);
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
    public void Forecasts_follow_the_Due_Date_as_if_each_occurrence_is_done_on_its_Due_Date()
    {
        RecurringChore laundry = NewChore(1, IntervalUnit.Week, DayOfWeek.Tuesday);
        laundry.Complete(new DateOnly(2027, 3, 11));

        Assert.Equal(
            [new DateOnly(2027, 3, 23), new DateOnly(2027, 3, 30), new DateOnly(2027, 4, 6)],
            laundry.Forecasts(until: new DateOnly(2027, 4, 6)));
    }

    [Fact]
    public void Forecasts_of_a_monthly_Chore_move_each_occurrence_to_the_nearest_Preferred_Day()
    {
        RecurringChore chore = NewChore(1, IntervalUnit.Month, DayOfWeek.Saturday);
        chore.Complete(new DateOnly(2027, 3, 15));

        // Due Saturday 17 April; 17 May is a Monday, 15 June a Tuesday
        Assert.Equal(
            [new DateOnly(2027, 5, 15), new DateOnly(2027, 6, 12)],
            chore.Forecasts(until: new DateOnly(2027, 6, 30)));
    }

    [Fact]
    public void Forecasts_skip_occurrences_outside_the_Season()
    {
        RecurringChore mowing = NewChore(2, IntervalUnit.Week, DayOfWeek.Saturday);
        mowing.Season = new Season(4, 10);
        mowing.Complete(new DateOnly(2027, 10, 9));

        // Due Saturday 23 October; the next fortnightly Saturdays inside the Season
        Assert.Equal(
            [new DateOnly(2028, 4, 8), new DateOnly(2028, 4, 22)],
            mowing.Forecasts(until: new DateOnly(2028, 4, 30)));
    }

    [Fact]
    public void Forecasts_start_after_a_Postponed_Due_Date()
    {
        RecurringChore laundry = NewChore(1, IntervalUnit.Week, DayOfWeek.Tuesday);
        laundry.Complete(new DateOnly(2027, 3, 11));
        laundry.Postpone(new DateOnly(2027, 3, 20));

        Assert.Equal(
            [new DateOnly(2027, 3, 30), new DateOnly(2027, 4, 6)],
            laundry.Forecasts(until: new DateOnly(2027, 4, 6)));
    }

    [Fact]
    public void No_Forecasts_before_the_next_occurrence()
    {
        RecurringChore laundry = NewChore(1, IntervalUnit.Week, DayOfWeek.Tuesday);
        laundry.Complete(new DateOnly(2027, 3, 11));

        Assert.Empty(laundry.Forecasts(until: new DateOnly(2027, 3, 22)));
    }

    [Fact]
    public void Interval_must_be_at_least_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Interval(0, IntervalUnit.Day));
    }
}
