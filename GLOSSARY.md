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
A record that a Chore was done on a given date; the history of a Chore is its list of Completions.
_Avoid_: Check-off, log entry

### Scheduling

**Interval**:
The roughly-desired number of days between two completions of a Recurring Chore.
_Avoid_: Frequency, recurrence

**Preferred Day**:
The weekday a Recurring Chore would ideally be done on, such as Tuesday for laundry.

**Last Done**:
The date of a Chore's most recent Completion.
_Avoid_: Completed at, last completion

**Due Date**:
The date a Chore should be done by; for a Recurring Chore, Last Done plus the Interval, moved to the nearest Preferred Day.
_Avoid_: Deadline

**Postpone**:
To set a Chore's Due Date by hand, overriding the calculated one until its next Completion.
_Avoid_: Snooze, skip, defer

**Overdue**:
The state of a Chore whose Due Date has passed; independent of Importance.

**Urgency**:
A score for how pressing a Chore is right now: progress from Last Done towards the Due Date (1 on the Due Date), weighted by Importance.
_Avoid_: Freshness, priority

**Forecast**:
An expected future occurrence of a Recurring Chore beyond its Due Date, assuming each occurrence is done exactly on its Due Date.
_Avoid_: Projection, occurrence, instance

### Seasons

**Season**:
The part of the year in which a Recurring Chore applies, such as spring to autumn for mowing the lawn.

**Dormant**:
The state of a Recurring Chore outside its Season: hidden and never Overdue.
_Avoid_: Paused, inactive
