# Due Date follows Last Done, not a fixed calendar

Todoist-style recurrence ("every Tuesday") keeps firing on the calendar regardless of when a chore was actually done, which is what made its recurring overview useless for us. Instead, a Recurring Chore's Due Date is derived from its Last Done plus its Interval, moved to the nearest Preferred Day when one is set; Last Done is always the latest Completion and is never stored separately. Doing laundry two days late on Thursday therefore makes it due the next Tuesday (five days later), and undoing a Completion simply restores the previous Due Date.

## Consequences

The future is not stored: the week planner and month calendar compute Forecasts by assuming each occurrence is done on its Due Date, so they shift whenever a Chore is completed early or late.
