# Devcontainer

Status: ready-for-agent
Blocked by: 01

A devcontainer in which the whole app can be built, tested and run. The primary workflow is a terminal with Neovim; VS Code must work just as well, so other contributors can join without adopting that setup. A fresh machine (or a coding agent) needs only Docker and either the `devcontainer` CLI or VS Code.

## Acceptance

- `.devcontainer/devcontainer.json` based on the official .NET 10 devcontainer image, with the Node LTS feature and the GitHub CLI feature.
- **Terminal workflow**: `devcontainer up --workspace-folder .` starts it, `devcontainer exec --workspace-folder . zsh` (or bash) opens a shell. Both commands are in the README, optionally wrapped in a tiny script (`./dev` or a `Makefile` target).
- **Neovim inside the container**: a recent Neovim (≥ 0.10, not the outdated distro package) plus `git`, `ripgrep`, `fd` and a C compiler for Treesitter.
- **VS Code**: "Reopen in Container" works out of the box. `customizations.vscode.extensions` installs C# Dev Kit, ESLint, Prettier and EditorConfig, and the repo ships a shared `.vscode/launch.json` for debugging the API.
- **Your own Neovim config**: the host's `~/.config/nvim` is bind-mounted read-only. The mount must not break the container when that folder doesn't exist on a contributor's machine (for example, mount it from a script or `initializeCommand` that creates the folder first). Plugin and Mason data (`~/.local/share/nvim`) live on a named volume, so plugins survive rebuilding the container.
- **Language servers in the container**, so they see the .NET SDK and `node_modules`: a C# server (`roslyn` language server or `csharp-ls`), `typescript-language-server`, and `vscode-langservers-extracted` for JSON/CSS/HTML. Installing them through Mason from the mounted config is fine. The README notes which ones are expected.
- Forwarded/published ports for the API and the Vite dev server, reachable from the host browser.
- `postCreateCommand` restores .NET and npm dependencies once they exist, and is a no-op before issue 03.
- The SQLite data directory lives inside the workspace (git-ignored), so data survives rebuilding the container.
- Git commits and pushes can keep happening on the host, since the workspace is bind-mounted. No SSH agent forwarding is required.
- README "Develop" section with two short routes: **Terminal + Neovim** (install the CLI with `npm i -g @devcontainers/cli`, start, enter) and **VS Code** (install the Dev Containers extension, then "Reopen in Container"). Both are followed by the same commands to build, test and run.
