# Dev environment

Everything runs in the devcontainer: `dotnet`, `npm` and the app, through `./dev <command>`. From a worktree, `./dev` runs in the main checkout's devcontainer, in the worktree's folder.

## Checking

`./check` runs everything a PR must pass and CI runs the same script. It prints one `ok`/`FAIL` line per step, with only the failing step's errors. Run it before every commit, alone, as a plain command; chained shell commands trip the worktree guard.

## Seeing it run

Green tests have never caught a layout, navigation or empty-state bug in this repo; only the running app shows them. A change to `web/` gets two looks:

- **Implementer:** before handing off, see that each touched screen renders at **phone width (390×844)** and **desktop width (1280×800)**.
- **Review:** `/experience-review`, run after `/code-review`, walks every touched screen and state at both widths, and its screenshots go in the PR's Evidence.

The recipe, for both:

1. Ports 5080 (API) and 5173 (Vite) are shared by every worktree: `curl -s -o /dev/null -w "%{http_code}" localhost:5173`. Anything but `000` means another worktree is serving; ask the user before stopping it.
2. Start both as background commands:
   - `./dev dotnet run --project src/Chores.Api --launch-profile http`: Development, with the database at `data/chores.db` in the worktree. For a throwaway one, prefix `env Database__Path=/tmp/<name>/chores.db` after `./dev`.
   - `./dev npm run dev --prefix web`, after `./check` or `./dev npm ci --prefix web` so `node_modules` are Linux builds.
3. Create data through the API with the header `X-Api-Key: dev-api-key`, e.g. `curl -H "X-Api-Key: dev-api-key" -H "Content-Type: application/json" -d '{"name":"Kitchen"}' localhost:5080/api/areas`. Request shapes are in `src/Chores.Api/openapi.json`.
4. Open <http://localhost:5173> with agent-browser and log in with password `chores`. Port 5080 serves the last *built* front end, not your change.
5. Stop both when done.

## After a PR merges

The `clean-up` skill removes merged worktrees, their branches and the servers they left running in the devcontainer.
