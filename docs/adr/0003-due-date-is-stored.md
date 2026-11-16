# The current Due Date is stored

ADR-0002 derives a Recurring Chore's Due Date from its Last Done, and ticket 05 kept derived values out of the database. That makes simple questions such as "which Chores are due this week?" impossible to answer in SQL, so the current Due Date is stored after all. The Domain stays its only author: every change that affects it (a Completion or Skip added or removed, a Postpone, a new Interval, Preferred Day or Season) recalculates it, and nothing else writes it. The Due Date no longer depends on today (Seasons skip occurrences rather than hide the Due Date), which is what makes it storable.

## Considered Options

- **Calculate on read, filter in memory**: one household has at most a few hundred Chores, so it would be fast enough, but every query has to load all Chores with their Completions.

## Consequences

Last Done and Urgency stay derived, and Forecasts beyond the current Due Date are still not stored. Any code path that changes a Recurring Chore must go through the Domain, or the stored Due Date can drift. Any Completion, Skip or removal of either ends a Postpone. Undoing a Completion therefore restores the calculated Due Date, not an earlier Postpone.
