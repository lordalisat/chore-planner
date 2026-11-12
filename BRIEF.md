# Brief: household & habits planner

Starting context for planning this project. Nothing here is decided beyond the
"Decided" list — structure, tooling and features are still open.

## Why

- Learning project to improve my general C# skills. I don't necessarily need to
  write the code myself, but want to be involved in the architecture decisions
- Replaces Todoist for household and recurring tasks. Todoist's overview of
  recurring tasks doesn't work for me.

## Decided

- Used mostly on my phone.
- Replaces Todoist (no ongoing sync needed; a one-time import is optional).
- Shared with my girlfriend, but no accounts needed for now.

## Ideas so far (not commitments)

- **Freshness instead of due dates**: each task has an interval ("roughly every
  7 days") and a last-done date; urgency = days since done ÷ interval.
- Overview of what needs attention, coloured from fresh to overdue.
- Week planner that shows how much work lands on each day.
- History per task, to see how consistent we actually are.
- Grouping by room/area; a "15 minutes free?" mode.
- Track who completed a task.
- Reminders on the phone.
- Tasks based on time of year (e.g. yard work)
- Easy interface for AI tooling to add tasks

## To decide in planning

- Project structure and architecture.
- Front end (e.g. Blazor, Angular, MAUI, PWA) and back end.
- Storage, hosting, how sharing works without accounts.
- Feature scope for a first version, and the order to build things in.
- How Claude should help (guide and review vs. write code).
