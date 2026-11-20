# Dev environment

The host's .NET SDK doesn't match `global.json`, so run everything through `./dev <command>`, never bare `dotnet`. From a worktree, `./dev` runs in the main checkout's devcontainer, in the worktree's folder.

## Checking

`./check` runs everything a PR must pass and CI runs the same script. It prints one `ok`/`FAIL` line per step, with only the failing step's errors. Run it before every commit, alone, as a plain command; chained shell commands trip the worktree guard.

## Seeing it run

A change to `web/` is done when you have seen it in a browser at **phone width (390×844)** and **desktop width (1280×800)**, walked every screen and state it touches, and put the screenshots in the PR's Evidence. Green tests are not this check: they have never caught a layout, navigation or empty-state bug in this repo.

1. Ports 5080 (API) and 5173 (Vite) are shared by every worktree: `curl -s -o /dev/null -w "%{http_code}" localhost:5173`. Anything but `000` means another worktree is serving; ask the user before stopping it.
2. Start both as background commands:
   - `./dev dotnet run --project src/Chores.Api --launch-profile http` (Development; the database is `data/chores.db` in the worktree)
   - `./dev npm run dev --prefix web` (after `./check` or `./dev npm ci --prefix web`, so `node_modules` are Linux builds)
3. Open <http://localhost:5173> with agent-browser and log in with password `chores`. Port 5080 serves the last *built* front end, not your change.
4. Stop both when done.

## After a PR merges

PRs are squash-merged, so `ExitWorktree remove` always reports unmerged commits. Confirm with `gh pr view <number> --json state` that it says `MERGED`, then remove with `discard_changes: true`. Worktrees share the main container, so there are no containers or images to remove.
