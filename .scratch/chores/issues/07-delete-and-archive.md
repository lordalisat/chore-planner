# Deleting Chores: Archive or hard delete

Status: ready-for-agent
Blocked by: 02

Keep history: a Chore with Completions is Archived rather than removed (see `spec.md`, Deleting).

## Domain

- Add **Archived** to `GLOSSARY.md`: a Chore removed by the household whose Completions are kept for history. _Avoid_: deleted, hidden, inactive.
- `Chore.Archive()` and `Chore.Restore()`; an Archived Chore is never Overdue and has no Urgency. Domain tests for both.
- EF mapping and a migration for the archived state.

## API

- `DELETE /api/chores/{id}`: archives when the Chore has Completions, hard-deletes otherwise; the response says which (for example `{ archived: true }`).
- `POST /api/chores/{id}/restore`: un-archives; 404 if not Archived.
- `GET /api/chores` leaves out Archived Chores; `GET /api/chores/{id}` answers 404 for one.

## UI

- **Delete** in the Chore edit screen.
- When the Chore has Completions: archive, return to the Overview and show an Undo toast that restores it; no confirmation.
- When it has none: confirm first, since a hard delete has no Undo. The DTO tells the client which case applies (Last Done, or a Completion count).

## Acceptance

- Integration tests: delete without Completions removes the row; with Completions it archives and keeps them; restore brings it back with its history; Archived Chores are not listed.
- On a phone: deleting laundry (done before) shows Undo; deleting a fresh Chore asks to confirm.
