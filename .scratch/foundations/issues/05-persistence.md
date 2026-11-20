# Persistence with EF Core and SQLite

Status: resolved
Blocked by: 04

Persist the Domain model with EF Core and SQLite in `Chores.Api`. The Domain project stays free of EF references; mapping lives in the Api project.

## Acceptance

- Chores, Completions and Areas (with optional parent Area) round-trip through the database. Covered by tests against a real SQLite database (in-memory or temporary file).
- Migrations live in the repo and are applied on startup.
- The database path comes from configuration and defaults to a file under a data directory suitable for a Docker volume.
- Derived values (Last Done, Due Date, Urgency) are not stored as columns.

## Comments

**From 04 (scheduling rules):** `Chore` is abstract with `RecurringChore` and `OneOffChore` subclasses (a natural fit for TPH). Completions sit in a private `_completions` list exposed as `IReadOnlyList<Completion>`; `Completion` is a value record (date, Due Date, Postpone date, skipped flag) without an identity yet, so give it a key here; a Skip is a Completion with `Skipped` set, so there is no separate table. `RecurringChore` stores `PostponedTo` (`private set`), which must be persisted, as it is not derived. Its `DueDate` is calculated and not stored. `OneOffChore.DueDate` is a plain nullable property, set by hand, so it is stored. `Interval` and `Season` are value records (owned/complex types), `Area` is an entity with an optional parent.
