# Areas

Status: ready-for-agent
Blocked by: 04

## API

- `GET /api/areas`: all Areas with id, Name, parent id and how many Chores use each (for the delete confirmation).
- `POST /api/areas`, `PUT /api/areas/{id}`: Name and optional parent; 400 on an empty name or a cycle (an Area can't be its own ancestor).
- `DELETE /api/areas/{id}`: hard delete; Chores using it (Archived ones included) and child Areas are detached.
- Chore create and edit endpoints accept an optional `areaId` (400 if unknown); Chore DTOs already carry the Area.

## UI

- **Areas** screen from the menu: a flat list showing each Area's parent; create, rename, set or clear the parent, and delete with a confirmation naming how many Chores use it.
- An Area picker in the Chore form; Overview rows show the Area name, and so does the All Chores screen (09) as a column, whichever of 08 and 09 lands last adds it.
- No filtering or grouping by Area (later effort).

## Acceptance

- Integration tests: create, rename, re-parent, delete; cycle rejected; deleting an Area detaches its Chores and children; unknown `areaId` on a Chore is 400.
- On a phone and in a desktop browser: create "Kitchen" inside "Downstairs", assign it to "Clean fridge", see it on the Overview row.
