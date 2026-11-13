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
  .devcontainer/             .NET 10 + Node LTS development environment
  Dockerfile                 one image: API + static front end
  README.md, .gitignore, .gitattributes, .editorconfig, global.json, Directory.Build.props
  ```
- **Development happens in a devcontainer**, primarily driven from the terminal with the `devcontainer` CLI to build, test and run, while editing happens on the host, with VS Code as an equally supported route for other contributors. The only host requirements are Docker plus either the CLI or VS Code.
- **Dates only**, no times. "Today" is the current date in Europe/Amsterdam, obtained through an injectable clock (`TimeProvider`) so tests can fix it.
- **Storage**: SQLite file in a Docker volume, EF Core migrations applied on startup.
- **Auth**: one household password (hash in configuration) issuing a cookie valid for a year; one API key in an `X-Api-Key` header for AI tooling. Every `/api` endpoint except login requires one of the two.
- **Configuration** through environment variables: password hash, API key, database path.
- The API exposes an **OpenAPI document**, so AI tooling can discover it.

## Out of scope

Chore screens, overview, history, planner, calendar, nested-Area filtering, MCP, reminders, Todoist import.

## Issues

1. `01-repository-setup.md`
2. `02-devcontainer.md` (after 01)
3. `03-solution-skeleton.md` (after 02)
4. `04-scheduling-rules.md` (after 03)
5. `05-persistence.md` (after 04)
6. `06-authentication.md` (after 03)
7. `07-docker-image.md` (after 03)
