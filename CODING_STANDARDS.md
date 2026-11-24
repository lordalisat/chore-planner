# Coding standards

Judgement calls for review. Anything mechanical is a check in `./check` (`.editorconfig` style, oxlint rules), not a rule here.

## Domain language

Code, UI text, docs and commits use the terms in `GLOSSARY.md` with its capitalisation (Last Done, Due Date, Overdue), never a word it lists under _Avoid_. A concept the glossary lacks is a question for the user, not a new word.

## Concepts that grow

A domain concept that will gain kinds (a ticket, spec or `.scratch/` file hints at more variants) is a closed record hierarchy carrying its own behaviour, like `PreferredDay` with `Weekday`, `FirstOfMonth` and `LastOfMonth`. Not an enum, and stored as text through a value converter rather than integer codes, so a new kind needs no data migration. Helpers about the concept live on the type, not in its callers.

## API requests and responses

- One file per DTO in `src/Chores.Api/Dtos/`, named after the type, never several DTOs per file and never nested in an endpoint class.
- A request DTO validates itself and returns a `Result<T>`: it collects a local `List<FieldError>` (`errors.Try(field, message, build)` wraps throwing Domain constructors) and ends in `return new Result<T>(errors, () => build)`. It doesn't take an error collection to fill. Facts from the database (taken Names) come in as arguments.
- Endpoints stay thin: load, `if (result.Value is not { } x) return result.ToProblem();`, save, answer.

## Seed data

A change that adds a state a screen can show (a new kind of Preferred Day, a new Overview section, a new mark on a row) adds a Chore in that state to `./seed`, so every later review gets it in one step.

## Evidence in the PR

- A change under `web/` says what `/experience-review` saw at phone and desktop width on every screen it touches: its hard findings (none, or how they were fixed) and what was checked. Screenshots stay with the review; `gh` can't attach them.
- A change to the build environment (`.devcontainer/`, `Dockerfile`, `compose.yaml`, `dev`, `check`, `seed`, `serve`, `.github/`) shows output of it actually running: `devcontainer up`, `docker build`, a CI run. A syntax check isn't evidence.
