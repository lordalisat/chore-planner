# Editing a Chore

Status: ready-for-agent
Blocked by: 02

## API

- `GET /api/chores/{id}`: one Chore DTO; 404 if unknown.
- `PUT /api/chores/{id}`: replaces the editable fields for its kind (Recurring: Name, Interval, Preferred Day, Season, Importance, Effort; One-off: Name, Due Date, Importance, Effort). The kind can't change. Same 400 rules as creating. Answers the updated DTO.

## UI

- Tapping an Overview row opens the Chore form at the `/chores/$id` route (typed numeric param), filled in, reusing the create form. Its loader prefetches the Chore through TanStack Query. Saving returns to the Overview.
- Last done is not editable here; Completions are how it changes.

## Acceptance

- Integration tests: edit each kind; 400 on an invalid combination; 404 on an unknown id; the Due Date in the response reflects a new Interval or Preferred Day.
- On a phone and in a desktop browser: changing laundry from Tuesday to Saturday moves its Due Date on the Overview.
