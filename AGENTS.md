## Running and checking

Run `dotnet`, `npm` and the app through `./dev`; check with `./check`; a `web/` change is seen in a browser at phone and desktop width. See `docs/agents/dev-environment.md`.

## Reviewing

A review of a diff that touches `web/` runs `/experience-review` after `/code-review`, and reports both.

## Agent skills

### Issue tracker

Issues live as local markdown files under `.scratch/<feature>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Default vocabulary (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`), recorded as a `Status:` line in each issue file. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `GLOSSARY.md` and `docs/adr/` at the repo root. See `docs/agents/domain.md`.
