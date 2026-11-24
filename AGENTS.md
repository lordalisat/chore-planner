## Running and checking

Run `dotnet`, `npm` and the app through `./dev`; check with `./check`; a `web/` change is seen in a browser at phone and desktop width. See `docs/agents/dev-environment.md`.

## Before a PR

1. `./check` passes.
2. `/code-review` against `main`.
3. For a diff touching `web/`: `/experience-review` after it.
4. `/pr` writes the body, with what both reviews found as Evidence.

## Agent skills

### Issue tracker

Issues live as local markdown files under `.scratch/<feature>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Default vocabulary (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`), recorded as a `Status:` line in each issue file. See `docs/agents/triage-labels.md`.

### UI SDK

Before using ui-sdk, follow the `ui-sdk` skill if it's installed, and read `web/node_modules/@ui-sdk/react/docs/llms.txt`. To build a new component, also search `web/node_modules/@ui-sdk/react/docs/core.md` by heading, and read `rendering.md` next to it. Where the skill says to drop JSDoc and most comments, keep this repo's style: its JSDoc and why-comments stay.

### Domain docs

Single-context: one `GLOSSARY.md` and `docs/adr/` at the repo root. See `docs/agents/domain.md`.
