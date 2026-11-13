# Chores

A shared planner for the household work of one household, replacing Todoist for chores. Every Recurring Chore has an Interval and a Last Done date, so the app can show at a glance what needs attention, ranked by Urgency up to Overdue. It runs as a password-protected PWA on a home server, is used mostly from a phone, and exposes an API that AI tooling can use to add Chores.

- [`BRIEF.md`](BRIEF.md): why this project exists and the ideas behind it
- [`GLOSSARY.md`](GLOSSARY.md): the domain language used in code and docs
- [`docs/adr/`](docs/adr/): architecture decision records
- [`.scratch/`](.scratch/): specs and issues, one folder per feature

## Develop

The app is built, tested and run inside a devcontainer (.NET 10, Node LTS, GitHub CLI). On the host you only need Docker (or Podman) and one of the two routes below. The workspace is bind-mounted, so editing and git stay on the host.

**Terminal**

```sh
npm i -g @devcontainers/cli
devcontainer up --workspace-folder .          # start (builds on first run)
devcontainer exec --workspace-folder . zsh    # open a shell inside
```

`./dev` does both in one go, and `./dev <command>` runs a single command inside. Edit with any editor on the host, in another terminal pane; editor language servers then need the .NET SDK and Node on the host.

**Podman** works instead of Docker, as long as a `docker` command reaches it (Podman's Docker shim or `podman-docker`), or pass `--docker-path podman` to the CLI. Rootless Podman also needs `PODMAN_USERNS=keep-id:uid=1000,gid=1000`, otherwise the workspace isn't writable inside the container. Its builder also can't install features through BuildKit, so pass `--buildkit never` to `devcontainer up`. `./dev` does both for you; when calling `devcontainer` or VS Code directly, set them yourself. On macOS the Podman machine must share the repo folder with its VM.

**VS Code**

Install the Dev Containers extension, open the repo and run "Dev Containers: Reopen in Container". The C# Dev Kit, ESLint, Prettier and EditorConfig extensions install automatically, and the shared `.vscode/launch.json` debugs the API.

**Inside the container**

_Build, test and run commands follow with the solution skeleton._

The API is published on <http://localhost:5080> and the Vite dev server on <http://localhost:5173>. Local SQLite data lives in `data/` in the workspace (git-ignored), so it survives rebuilding the container.

## Run

_To be filled in by the Docker image issue._
