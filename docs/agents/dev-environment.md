# Dev environment

Everything runs in the devcontainer: `dotnet`, `npm` and the app, through `./dev <command>`. From a worktree, `./dev` runs in the main checkout's devcontainer, in the worktree's folder.

## Checking

`./check` runs everything a PR must pass and CI runs the same script. It prints one `ok`/`FAIL` line per step, with only the failing step's errors. Run it before every commit, alone, as a plain command; chained shell commands trip the worktree guard. So does any command whose text it can't verify, such as inline Python or a heredoc that mentions git: put a scripted or multi-line edit in a file under the job's tmp folder and run that.

A new worktree has no `web/node_modules`, so no UI SDK docs either, until `./dev npm ci --prefix web` (or the first `./check`) installs them.

## Seeing it run

Green tests have never caught a layout, navigation or empty-state bug in this repo; only the running app shows them. A change to `web/` gets two looks:

- **Implementer:** before handing off, see that each touched screen renders at **phone width (390×844)** and **desktop width (1280×800)**.
- **Review:** `/experience-review`, run after `/code-review`, walks every touched screen and state at both widths. Its screenshots stay with its report; the PR's Evidence says in words what it saw.

The recipe, for both, and for a hand test the user asks for:

1. `./serve` starts the API on a fresh throwaway database filled by `./seed` (Areas and a Chore in every state, dated from today) and Vite, and returns once both answer. It refuses while ports 5080/5173 are taken by another checkout; ask the user before stopping theirs.
2. Open <http://localhost:5173> with agent-browser and log in with password `chores`. Port 5080 serves the last *built* front end, not your change. For a state the seed lacks, call the API with the header `X-Api-Key: dev-api-key`; request shapes are in `src/Chores.Api/openapi.json`. To run JavaScript in the page, put it in a file and run `agent-browser eval --stdin < <file>` from a script file too: the guard refuses a command line containing `eval`.
3. `./serve stop` when done; the logs are in `data/serve/`.

## After a PR merges

The `clean-up` skill removes merged worktrees, their branches and the servers they left running in the devcontainer.
