# Devcontainer

Status: ready-for-agent
Blocked by: 01

A devcontainer in which the whole app can be built, tested and run. The primary workflow is a terminal running the devcontainer CLI, with editing on the host; VS Code must work just as well, so other contributors can join without adopting that setup. A fresh machine (or a coding agent) needs only Docker and either the `devcontainer` CLI or VS Code.

## Acceptance

- `.devcontainer/devcontainer.json` based on the official .NET 10 devcontainer image, with the Node LTS feature and the GitHub CLI feature.
- **Terminal workflow**: `devcontainer up --workspace-folder .` starts it, `devcontainer exec --workspace-folder . zsh` (or bash) opens a shell. Both commands are in the README, optionally wrapped in a tiny script (`./dev` or a `Makefile` target).
- **VS Code**: "Reopen in Container" works out of the box. `customizations.vscode.extensions` installs C# Dev Kit, ESLint, Prettier and EditorConfig, and the repo ships a shared `.vscode/launch.json` for debugging the API.
- Forwarded/published ports for the API and the Vite dev server, reachable from the host browser.
- `postCreateCommand` restores .NET and npm dependencies once they exist, and is a no-op before issue 03.
- The SQLite data directory lives inside the workspace (git-ignored), so data survives rebuilding the container.
- Git commits and pushes can keep happening on the host, since the workspace is bind-mounted. No SSH agent forwarding is required.
- README "Develop" section with two short routes: **Terminal** (install the CLI with `npm i -g @devcontainers/cli`, start, enter) and **VS Code** (install the Dev Containers extension, then "Reopen in Container"). Both are followed by the same commands to build, test and run.

## Comments

**2026-11-13, during implementation:** the editor moved out of the container. The container only builds, tests and runs the app; editing happens on the host with any editor. Mounting host editor config into the container proved fragile (sandboxed shells and a Podman VM that only shares the projects folder could not see it), so the in-container editor, config-mount and language-server criteria were dropped.
