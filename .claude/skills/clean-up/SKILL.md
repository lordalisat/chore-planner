---
name: clean-up
description: Clean up after merged PRs. Use when the user says a PR is merged, or asks to clean up worktrees, branches or the dev environment.
---

A worktree is **done** when its branch is an ancestor of `origin/main`: PRs are merge commits, so that holds after every merge. `ExitWorktree` counts commits, not merges, so it reports "N commits" even for a done worktree; ancestry is the test.

1. **Leave your own worktree.** If this session is in one:
   - `git fetch -q origin`, then `git merge-base --is-ancestor HEAD origin/main`.
   - Exit 0 means done. Run `.claude/skills/clean-up/clean-up --stop-here` as a plain command to stop what this worktree still runs in the devcontainer, such as servers for a hand test (`ExitWorktree` refuses while they run), then `ExitWorktree` with `action: remove`, `discard_changes: true`.
   - Any other exit: say it isn't merged and stop.
2. **Sweep.** From the main checkout, run `.claude/skills/clean-up/clean-up` as a plain command. For every done worktree that no running session has locked, it:
   - stops its processes in the devcontainer
   - removes the worktree and its branch

   It also deletes merged branches that GitHub has deleted and no worktree has checked out, stops processes left behind by worktrees that are already gone, removes old per-worktree devcontainers, and fast-forwards `main` when it's checked out and clean. `-n` only prints the plan.
3. **Report** from the script's output: what was removed, what was kept and why. If the script kept a worktree as "locked", it belongs to a live session; leave it alone.
