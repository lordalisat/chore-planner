# Chores

Step 2 of the build order, after Foundations: the first version the household can use from the phone, replacing Todoist for chores. Vocabulary follows `GLOSSARY.md`; see also `docs/adr/`.

## Goal

From the PWA, the household can see what needs doing ordered by Urgency, tick Chores off (Complete, Skip, Postpone, Undo), and keep the list of Chores and Areas up to date. The Domain and persistence from Foundations exist; this effort adds the API endpoints and the front end on top.

## Decisions

### Scope

- **In:** Overview by Urgency; Complete (today or another day), Skip and Postpone from the Overview with Undo; create, edit and delete Recurring and One-off Chores with all Domain fields (Name, Importance, Effort, Area, Interval, Preferred Day, Season, Due Date); manage Areas (flat screen, optional parent).
- **Later efforts:** History, week planner and Forecasts, nested-Area filtering, "15 minutes free?", who completed a Chore, reminders, Todoist import, an Archived Chores screen, offline actions, live push updates.
- **Slicing:** vertical slices, each ticket ships its endpoint(s), integration tests and the UI using them.

### API

- Hand-written minimal-API endpoints under `/api` with request/response DTOs; Domain types are never serialized directly.
- **The server computes scheduling.** Each Chore in a response carries Due Date, Urgency, Overdue, Dormant and Last Done as computed by the Domain with the server clock (Europe/Amsterdam). The front end only displays them and sends commands; it never repeats scheduling rules.
- **Preferred Day** is a string in JSON, the same text as in the database: `"Monday"`…`"Sunday"`, `"FirstOfMonth"`, `"LastOfMonth"`, or `null`, documented in OpenAPI as a string enum. **Interval** is `{ count, unit }` with unit `"Day" | "Week" | "Month" | "Year"`. Rule violations the Domain rejects (such as `FirstOfMonth` on a weekly Interval) answer 400 with a validation problem keyed by field, which the form shows at that field.
- **Commands carry the Due Date the device saw.** Complete, Skip and Postpone send `dueDate`; when it no longer matches the Chore's current Due Date (someone else acted first), the API answers 409 and changes nothing. This is the only concurrency guard; no version columns.
- **OpenAPI at build time:** `Microsoft.Extensions.ApiDescription.Server` writes `openapi.json` into the repo on build; `npm run api` in `web/` regenerates TypeScript types from it (`openapi-typescript`) for a small typed fetch client. Both are committed, so API changes show up as diffs.

### Deleting

- **Archived** (new glossary term): a Chore removed by the household whose Completions are kept for history. Archived Chores are hidden everywhere, are never Overdue and have no Urgency. The Domain gets `Archive()` and `Restore()`.
- `DELETE /api/chores/{id}` archives a Chore that has Completions and hard-deletes one that has none; the response says which. The UI button says "Delete" either way.
- Archiving shows an Undo toast (`POST /api/chores/{id}/restore`) and needs no confirmation; a hard delete has no Undo, so it asks for confirmation first.
- **Areas are hard-deleted**, after a confirmation naming how many Chores use it; their Chores (Archived ones included) and child Areas are detached.

### Front end

- React + Vite + TypeScript PWA as today, plus:
  - **UI SDK** (`@ui-sdk/react`, `@ui-sdk/core`) for interactive parts (dialog, select, date picker, menu, toast, field). It is beta and churns daily, so versions are pinned exactly (no `^`), and only components in `web/src/ui/` import `@ui-sdk/*`; screens use those wrappers.
  - **Tailwind v4** via `@tailwindcss/vite`, mobile-first, with tokens for the existing theme green `#2f6f4f` and cream `#f4f1ea`.
  - **TanStack Query** for all API calls; **TanStack Router** for screens, with code-based routes in one `router.tsx` (no file-based routing or generated route tree), loaders prefetching through TanStack Query, and a `beforeLoad` session check redirecting to the login; **no form library**: controlled state plus the UI SDK Field, with errors from the API's validation problems.
- **No live push (SSE) in v1.** Queries refetch when the app regains focus and after every mutation the device makes; the 409 guard covers two devices acting at once.
- **Undo instead of confirmation** for Complete, Skip and archiving: a toast for a few seconds. Completions are undone with `DELETE /api/chores/{id}/completions/{completionId}` (the Domain's `RemoveCompletion`).
- **Shell:** the Overview is the home screen; a floating **+** opens the new-Chore form; tapping a row opens it for editing; a small menu holds Areas and Log out. No tab bar.
- **Overview:** one list sorted by Urgency, highest first, Overdue rows marked; each row shows Name, Area, Effort and Due Date. Dormant Recurring Chores and finished One-off Chores are hidden; One-off Chores without a Due Date sit in an "Anytime" section at the bottom. No grouping by Area.
- **Dates:** relative in lists ("today", "tomorrow", "in 3 days", "2 days overdue", and "Tue 24 Nov" beyond a week); absolute in forms; weeks start on Monday.
- **English only**, matching the glossary; no i18n library.
- **Online only:** the service worker caches the app shell; when the API is unreachable the existing "server can't be reached" message shows.

### Testing

- API: integration tests on the existing `ApiFactory` for every endpoint, including 400, 401 and 409 paths.
- Front end: Vitest + Testing Library only for components with real logic (the Chore form's Interval / Preferred Day / Season rules, relative date formatting). No end-to-end browser suite in v1.

## Issues

1. `01-frontend-tracer.md`
2. `02-overview-and-recurring-chore.md` (after 01)
3. `03-one-off-chore.md` (after 02)
4. `04-edit-chore.md` (after 02)
5. `05-complete.md` (after 02)
6. `06-skip-and-postpone.md` (after 05)
7. `07-delete-and-archive.md` (after 02)
8. `08-areas.md` (after 04)
