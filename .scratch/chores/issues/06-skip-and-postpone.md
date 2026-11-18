# Skip and Postpone

Status: ready-for-agent
Blocked by: 05

## API

- `POST /api/chores/{id}/skip` with `{ dueDate }`: Recurring Chores only (400 for One-off). Same 409 guard as Complete. Answers the DTO with the skipped Completion's id.
- `POST /api/chores/{id}/postpone` with `{ dueDate, to }`: sets the Due Date by hand until the next Completion. Same 409 guard. `to` must be after today (400 otherwise).

## UI

- The row's actions gain **Skip** (Recurring only) and **Postpone…** (date picker).
- Skip shows the same Undo toast as Done, deleting the skipped Completion. Postpone needs no Undo: postponing again fixes a mistake.

## Acceptance

- Integration tests: Skip leaves Last Done unchanged and moves the Due Date on; Skip on a One-off Chore is 400; Postpone sets the Due Date and the next Completion clears it; 409 on a stale `dueDate` for both.
- On a phone and in a desktop browser: skipping laundry moves it a week on; Undo brings it back.
