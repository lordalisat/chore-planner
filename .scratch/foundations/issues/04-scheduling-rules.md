# Scheduling rules in the Domain

Status: ready-for-agent
Blocked by: 03

Model Chore (Recurring and One-off), Completion, Importance, Effort, Area and Season in `Chores.Domain` and implement the scheduling rules test-first. "Today" always comes from an injected clock.

## Rules

- **Last Done** is the date of the latest Completion; never stored separately. Removing a Completion falls back to the previous one.
- **Due Date of a Recurring Chore**:
  - Postponed: the Postpone date, until the next Completion.
  - With Last Done: Last Done + Interval; with a Preferred Day, moved to the nearest such weekday (offset −3..+3, never a tie). If that lands on or before Last Done, take the following Preferred Day instead.
  - Without any Completion: the first Preferred Day from the creation date, or the creation date when there is none.
- **Due Date of a One-off Chore**: optional, set by hand (Postpone changes it).
- **Overdue**: today is after the Due Date. Importance plays no part.
- **Season**: inclusive month range that may wrap the year (Nov–Feb). Outside it a Recurring Chore is **Dormant**: no Due Date, never Overdue, no Urgency.
- **Urgency** = progress × weight, with weight Low 0.75, Normal 1, High 1.5.
  - Progress = (today − cycle start) ÷ (Due Date − cycle start), where cycle start is Last Done, or the creation date when there is no Completion. It is 1 on the Due Date and above 1 when Overdue.
  - When the Due Date equals the cycle start, progress is 1.
  - A One-off Chore without a Due Date has progress 0.5.
- A completed One-off Chore is finished and has no Urgency.

## Acceptance

Each rule above has at least one test. These scenarios are tests verbatim:

- "Laundry, every 7 days, Tuesday", done on a Thursday → due the Tuesday 5 days later.
- Same Chore done on a Monday → due the Tuesday 8 days later.
- 7-day Interval, Normal, last done 10 days ago → Overdue, Urgency ≈ 1.43.
- Low, 14-day Interval, 2 days Overdue → Urgency ≈ 0.86; High, due today → 1.5.
- A Season of November–February, checked in January → not Dormant; in June → Dormant.
