# Completing a Chore from the Overview

Status: resolved
Blocked by: 02

## API

- `POST /api/chores/{id}/complete` with `{ dueDate, date? }`: `date` defaults to today (server clock). If `dueDate` doesn't match the Chore's current Due Date, answer **409** and change nothing (someone else acted first). Answers the updated DTO including the new Completion's id.
- `DELETE /api/chores/{id}/completions/{completionId}`: undoes a Completion (done or skipped) via `RemoveCompletion`. Completions need a stable id exposed for this.

## UI

- Each Overview row has **Done** (today) and a "Done on…" option with a date picker.
- After Done, an **Undo** toast shows for a few seconds; Undo deletes that Completion.
- On 409: refetch and show "Already done by someone else".
- After any mutation the Chore list is refetched; refetch on focus is already on from 01.

## Acceptance

- Integration tests: complete today and on an earlier date (the Due Date follows Last Done, ADR 0002); 409 on a stale `dueDate` with nothing recorded; undo restores the previous Due Date; a completed One-off Chore disappears from the list.
- On two devices: completing the same Chore on both records one Completion and shows the 409 message on the second.
