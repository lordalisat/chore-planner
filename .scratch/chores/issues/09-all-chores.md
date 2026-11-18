# All Chores screen

Status: ready-for-agent
Blocked by: 04, 07

The setup screen, mainly used from a desktop browser: every Chore in one place, including the ones the Overview hides. It also closes a gap: Dormant Chores are hidden from the Overview, so without this screen they can't be opened outside their Season.

## API

Nothing new: `GET /api/chores` (02, 03) already returns Dormant Chores, finished One-off Chores and Chores not due yet with their flags; `GET /api/chores?archived=true` (07) returns Archived ones with their archive date.

## UI

- **All Chores** at `/chores`, from the menu or top navigation.
- A filter: **Active** (default, every Chore that isn't Archived) or **Archived**.
- Active rows show Name, kind, Interval and Preferred Day, Season, Area (see 08), Importance, Effort and Due Date, with Dormant and finished Chores marked. On a phone: a plain list of rows. From the `md` breakpoint up: a table sortable by each column (sorted on the client).
- Tapping or clicking an Active row opens the Chore form (04). From the `lg` breakpoint up, the form opens **beside the list** (list left, form right), so you can click through Chores without leaving the list; the selected Chore is in the URL (for example `/chores/$id` rendered inside this layout, or a search param) so reloading keeps it. Below `lg`, the form opens as its own screen, as in 04.
- Creating from this screen at `lg` and up opens the empty form in the same side panel; "Save and add another" (02) works there too.
- Archived rows are **read-only**: Name, Area and the date archived, and one action, **Restore** (`POST /api/chores/{id}/restore`), after which the Chore is back under Active. No editing and no permanent delete.

## Acceptance

- In a desktop browser: sort by Area then by Name; click through three Chores editing each without leaving the list; enter five new Chores in a row with "Save and add another".
- A Dormant Chore ("Mow the lawn" in winter) appears here and can be edited.
- An Archived Chore appears under the Archived filter with its archive date, has no edit form, and Restore moves it back to Active and onto the Overview.
- On a phone: the list, filter and Restore work, and editing opens the full-screen form.
- Vitest: sorting by each column; the Archived filter shows Restore and no edit.
