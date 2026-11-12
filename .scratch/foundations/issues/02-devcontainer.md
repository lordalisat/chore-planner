# Devcontainer

Status: ready-for-agent
Blocked by: 01

A devcontainer in which the whole app can be built, tested and run, driven from a terminal with Neovim. A fresh machine (or a coding agent) needs only Docker and the `devcontainer` CLI.

## Acceptance

- `.devcontainer/devcontainer.json` based on the official .NET 10 devcontainer image, with the Node LTS feature and the GitHub CLI feature.
- **Terminal workflow**: `devcontainer up --workspace-folder .` starts it, `devcontainer exec --workspace-folder . zsh` (or bash) opens a shell. Both commands are in the README, optionally wrapped in a tiny script (`./dev` or a `Makefile` target).
- **Neovim inside the container**: a recent Neovim (≥ 0.10, not the outdated distro package) plus `git`, `ripgrep`, `fd` and a C compiler for Treesitter.
- **Your own Neovim config**: the host's `~/.config/nvim` is bind-mounted read-only. Plugin and Mason data (`~/.local/share/nvim`) live on a named volume, so plugins survive rebuilding the container.
- **Language servers in the container**, so they see the .NET SDK and `node_modules`: a C# server (`roslyn` language server or `csharp-ls`), `typescript-language-server`, and `vscode-langservers-extracted` for JSON/CSS/HTML. Installing them through Mason from the mounted config is fine. The README notes which ones are expected.
- Forwarded/published ports for the API and the Vite dev server, reachable from the host browser.
- `postCreateCommand` restores .NET and npm dependencies once they exist, and is a no-op before issue 03.
- The SQLite data directory lives inside the workspace (git-ignored), so data survives rebuilding the container.
- Git commits and pushes can keep happening on the host, since the workspace is bind-mounted. No SSH agent forwarding is required.
- README "Develop" section: install the CLI (`npm i -g @devcontainers/cli`), start, enter, then the commands to build, test and run.
