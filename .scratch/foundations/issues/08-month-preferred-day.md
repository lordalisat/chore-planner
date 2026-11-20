# First or last of month as Preferred Day

Status: resolved
Blocked by: 04

Besides a weekday, a Recurring Chore's Preferred Day can be "first of month" or "last of month". Raised in review of 04 (PR #5).

## Rules

- Only allowed when the Interval is in months or years; weekly or daily Chores keep weekday Preferred Days.
- The Due Date (Last Done + Interval) moves to the **nearest** first or last of month, just as a weekday Preferred Day moves to the nearest weekday. Ties go to the later date.
- If that lands on or before Last Done, take the following one instead.
- Without any Completion: the first such day from the creation date. Season handling is unchanged from 04.

## Acceptance

- "Every month, first of month", done 3 March → 3 April → due 1 April.
- Same Chore done 28 March → 28 April → due 1 May.
- "Every month, last of month", done 2 March → 2 April → due 31 March.
- A first- or last-of-month Preferred Day on a day or week Interval is rejected.
