# Foundations

Step 1 of the build order: everything needed before the first real feature (Chores, step 2) can be built on top. Vocabulary follows `GLOSSARY.md`; see also `docs/adr/0001-shared-household-password.md` and `docs/adr/0002-due-date-follows-last-done.md`.

## Goal

A running, deployable, password-protected app on the home server whose Domain already knows all scheduling rules, proven by tests. No user-facing Chore screens yet.

## Decisions

- **.NET 10 (LTS)**, C# with nullable reference types and modern features (records, pattern matching, primary constructors) used where they make the code clearer.
- **Solution layout**:
  ```
  src/Chores.Domain          pure C#: Chore, Completion, Area, scheduling, Urgency; no EF, no ASP.NET
  src/Chores.Api             minimal API, EF Core + SQLite, auth, serves the built front end
  tests/Chores.Domain.Tests  xUnit
  web/                       React + Vite + TypeScript PWA
  Dockerfile                 one image: API + static front end
  ```
- **Dates only**, no times. "Today" is the current date in Europe/Amsterdam, obtained through an injectable clock (`TimeProvider`) so tests can fix it.
- **Storage**: SQLite file in a Docker volume, EF Core migrations applied on startup.
- **Auth**: one household password (hash in configuration) issuing a cookie valid for a year; one API key in an `X-Api-Key` header for AI tooling. Every `/api` endpoint except login requires one of the two.
- **Configuration** through environment variables: password hash, API key, database path.
- The API exposes an **OpenAPI document**, so AI tooling can discover it.

## Out of scope

Chore screens, overview, history, planner, calendar, nested-Area filtering, MCP, reminders, Todoist import.

## Issues

1. `01-solution-skeleton.md`
2. `02-scheduling-rules.md`
3. `03-persistence.md`
4. `04-authentication.md`
5. `05-docker-image.md`
