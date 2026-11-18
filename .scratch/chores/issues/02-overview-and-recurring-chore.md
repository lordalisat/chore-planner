# Overview and creating a Recurring Chore

Status: ready-for-agent
Blocked by: 01

The first real slice: create Recurring Chores and see them on the Overview ordered by Urgency.

## API

- `GET /api/chores`: every Chore (Archived ones are left out once 07 lands), as DTOs carrying `id`, `kind` (`"Recurring" | "OneOff"`), Name, Importance, Effort, Area (id and name, or null), Interval, Preferred Day, Season, and the computed Due Date, Urgency, Overdue, Dormant and Last Done (see `spec.md`, API).
- `POST /api/chores/recurring`: Name, Interval, Preferred Day, Season, Importance, Effort, and an optional **Last done** date. When given, the Chore is created with a done Completion on that date, so moving over from Todoist gives the right Due Date immediately. Answers 201 with the Chore DTO.
- Domain rule violations (a month Preferred Day on a day or week Interval, an Interval count below 1, an invalid Season) answer 400 with a validation problem keyed by field. Area is left out until 08.
- A Name already used by another Chore (ignoring case and surrounding spaces) answers 400 at `name`, so the Overview never shows two Chores that look the same. Added while testing by hand; revisit per Area once 08 lands.
- A weekday Preferred Day needs an Interval of at least a week (so "every 7 days, Tuesday" still works, "every 1 day, Monday" answers 400 at `preferredDay`). Added while testing by hand; this narrows Foundations 08's "daily Chores keep weekday Preferred Days". The form hides Preferred Day when the Interval allows none.

## UI

- Overview at `/`: one list sorted by Urgency (highest first); Overdue rows marked; each row shows Name, Effort and relative Due Date ("today", "tomorrow", "in 3 days", "2 days overdue", "Tue 24 Nov" beyond a week). Dormant Chores are hidden. Empty state when there are no Chores.
- The **+** button opens the new-Chore form (for now only Recurring): Name, Interval (count + unit), Preferred Day (weekdays; first/last of month only offered for month or year Intervals), Season (from month, to month, optional), Importance, Effort in minutes, Last done (date picker, optional). Errors from the API appear at their field.
- Fast entry for setting up many Chores: the first field has focus when the form opens; Enter submits; besides Save, a **Save and add another** button keeps Interval, Preferred Day, Season and Area (once 08 lands) filled in, clears the rest, and focuses Name again.
- On wide screens the Overview stays one centered column.
- Relative date formatting lives in one tested function; weeks start on Monday.

## Acceptance

- Integration tests: create with and without Last done; computed fields match the Domain for a fixed clock; Dormant Chores are reported as Dormant; 400 for a month Preferred Day on a weekly Interval; 401 without a cookie.
- Vitest: the form hides first/last of month for day/week Intervals; "Save and add another" keeps Interval, Preferred Day and Season and clears Name; relative date formatting covers today, tomorrow, overdue, within a week, beyond a week.
- On a phone and in a desktop browser: creating "Laundry, every week, Tuesday" shows it on the Overview with the right Due Date.
