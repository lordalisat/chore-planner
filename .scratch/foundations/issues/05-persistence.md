# Persistence with EF Core and SQLite

Status: ready-for-agent
Blocked by: 04

Persist the Domain model with EF Core and SQLite in `Chores.Api`. The Domain project stays free of EF references; mapping lives in the Api project.

## Acceptance

- Chores, Completions and Areas (with optional parent Area) round-trip through the database. Covered by tests against a real SQLite database (in-memory or temporary file).
- Migrations live in the repo and are applied on startup.
- The database path comes from configuration and defaults to a file under a data directory suitable for a Docker volume.
- Derived values (Last Done, Due Date, Urgency) are not stored as columns.
