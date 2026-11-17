# Chores

A shared planner for the household work of one household, replacing Todoist for chores.

## Language

### Chores

**Chore**:
A piece of household work the household wants to keep track of; either a Recurring Chore or a One-off Chore.
_Avoid_: Task, habit, todo, errand

**Recurring Chore**:
A Chore that comes back after an Interval, such as vacuuming or doing laundry.

**One-off Chore**:
A Chore that is finished for good once completed, such as fixing a leaking tap.

**Importance**:
How much it matters that a Chore gets done: Low, Normal or High.
_Avoid_: Priority

**Effort**:
The estimated number of minutes a Chore takes.
_Avoid_: Duration, cost

**Area**:
A part of the house a Chore belongs to, such as the kitchen or the garden; an Area may sit inside one parent Area, such as the kitchen inside downstairs.
_Avoid_: Room, location, zone, area group

**Completion**:
A record that a Chore's Due Date was dealt with on a given date: done, or skipped (see Skip); the history of a Chore is its list of Completions.
_Avoid_: Check-off, log entry

### Scheduling

**Interval**:
How often a Recurring Chore should roughly be done: a whole number of days, weeks, months or years, such as "every 2 weeks" or "every 3 months".
_Avoid_: Frequency, recurrence

**Preferred Day**:
The day a Recurring Chore would ideally be done on: a weekday, such as Tuesday for laundry, or, for an Interval in months or years, the first or last of the month.

**Last Done**:
The date of a Chore's most recent Completion that was not skipped.
_Avoid_: Completed at, last completion

**Due Date**:
The date a Chore should be done by; for a Recurring Chore, Last Done plus the Interval, moved to the nearest Preferred Day.
_Avoid_: Deadline

**Postpone**:
To set a Chore's Due Date by hand, overriding the calculated one until its next Completion, done or skipped.
_Avoid_: Snooze, defer

**Skip**:
To let a Recurring Chore move on to its next Due Date as if it were done on the current one, recorded as a skipped Completion that leaves Last Done as it is; unlike Postpone, the new Due Date is calculated, not picked.
_Avoid_: Dismiss, pass

**Overdue**:
The state of a Chore whose Due Date has passed; independent of Importance.

**Urgency**:
A score for how pressing a Chore is right now, from how close today is to the Due Date and its Importance: 0 until a few days before the Due Date, rising to twice the Importance weight a few days after it.
_Avoid_: Freshness, priority

**Forecast**:
An expected future occurrence of a Recurring Chore beyond its Due Date, assuming each occurrence is done exactly on its Due Date.
_Avoid_: Projection, occurrence, instance

### Seasons

**Season**:
The part of the year in which a Recurring Chore applies, such as spring to autumn for mowing the lawn.

**Dormant**:
The state of a Recurring Chore outside its Season: hidden, with every occurrence that would fall in it skipped.
_Avoid_: Paused, inactive
