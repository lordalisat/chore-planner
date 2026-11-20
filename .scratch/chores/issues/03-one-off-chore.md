# Creating a One-off Chore

Status: resolved
Blocked by: 02

## API

- `POST /api/chores/one-off`: Name, optional Due Date, Importance, Effort. 201 with the Chore DTO.
- `GET /api/chores` includes finished One-off Chores with `finished: true`; the Overview hides them, the All Chores screen (09) shows them.

## UI

- The new-Chore form starts with a choice: Recurring or One-off. One-off shows Name, Due Date (optional), Importance, Effort.
- On the Overview, One-off Chores with a Due Date sort in by Urgency like any other; those without one go in an **Anytime** section at the bottom.

## Acceptance

- Integration tests: create with and without a Due Date; a finished One-off Chore is listed with `finished: true` and hidden from the Overview.
- On a phone and in a desktop browser: "Fix leaking tap" without a date appears under Anytime; with a date in two days it appears in the main list.
