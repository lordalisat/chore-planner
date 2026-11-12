# Devcontainer

Status: ready-for-agent
Blocked by: 01

A devcontainer in which the whole app can be built, tested and run, so a fresh machine (or a coding agent) needs only Docker and an editor.

## Acceptance

- `.devcontainer/devcontainer.json` based on the official .NET 10 devcontainer image, with the Node LTS feature and the GitHub CLI feature.
- VS Code extensions preinstalled: C# Dev Kit, ESLint, Prettier, EditorConfig. The setup also opens in Rider.
- Forwarded ports for the API and the Vite dev server.
- `postCreateCommand` restores .NET and npm dependencies once they exist, and is a no-op before issue 03.
- The SQLite data directory lives inside the workspace (git-ignored), so data survives rebuilding the container.
- README "Develop" section: open in the devcontainer, then the commands to build, test and run.
