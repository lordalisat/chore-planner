# A weekday in the month as Preferred Day

Builds on Foundations issue 04 and issue 08 (first and last of the month as Preferred Days). Grilled 2026-11-23; small enough to implement in one go, without tickets.

## Decisions

1. **A kind of Preferred Day, no new term.** The first, second, third, fourth or last Monday to Sunday of the month is one more kind of Preferred Day, next to the weekdays and the first and last of the month. A Chore has at most one Preferred Day. (Working name "Monthly Anchor" dropped.)
2. **Intervals in months or years only**, like the first and last of the month.
3. **Scheduling as for every Preferred Day** (ADR 0002): Last Done plus the Interval, moved to the nearest such day, even when that is earlier. Monthly "first Saturday" in 2027:
   - done on time, Sat 2 Jan → due Sat 6 Feb
   - done late, Sun 10 Jan → due Sat 6 Feb (10 Feb is nearest to 6 Feb)
   - done very late, Mon 25 Jan → due Sat 6 Mar
   - every 3 months, done 10 Jan → due Sat 3 Apr, a week before Last Done plus the Interval
4. **Seasons**: nothing new; a Due Date moved to a Season's start lands on the first such day on or after it.
5. **Form**: the Preferred day dropdown gains "A weekday in the month…", which shows **[First ▾] [Saturday ▾] of the month**. Switching to an Interval in days or weeks clears it, as it does for the first and last of the month.
6. **Lists** read "every month on the first Saturday", "every 3 months on the last Friday".
7. **Seed**: "Check the smoke alarms", monthly on the first Saturday, in Upstairs; Hall stays the Area without Chores.
