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

`./dev` does both in one go, and `./dev <command>` runs a single command inside. From a git worktree, `./dev` runs in the main checkout's container, in the worktree's folder, so worktrees share one container and its ports. Edit with any editor on the host, in another terminal pane; editor language servers then need the .NET SDK and Node on the host.

**Podman** works instead of Docker, as long as a `docker` command reaches it (Podman's Docker shim or `podman-docker`), or pass `--docker-path podman` to the CLI. Rootless Podman also needs `PODMAN_USERNS=keep-id:uid=1000,gid=1000`, otherwise the workspace isn't writable inside the container. Its builder also can't install features through BuildKit, so pass `--buildkit never` to `devcontainer up`. `./dev` does both for you; when calling `devcontainer` or VS Code directly, set them yourself. On macOS the Podman machine must share the repo folder with its VM.

**VS Code**

Install the Dev Containers extension, open the repo and run "Dev Containers: Reopen in Container". The C# Dev Kit, Oxlint, Prettier and EditorConfig extensions install automatically, and the shared `.vscode/launch.json` debugs the API.

**Inside the container**

```sh
./check                                    # everything a PR must pass: build, style, tests, web lint/types/tests (CI runs it too)
./serve                                    # the app on a fresh, seeded database for a hand test (./serve stop to end); from the host
./seed                                     # needs the API running on an empty database: adds Areas and a Chore in every state
dotnet build                               # API, Domain and tests (Chores.slnx)
dotnet test                                # all test projects
dotnet run --project src/Chores.Api        # API on :5080, OpenAPI at /openapi/v1.json
dotnet ef migrations add <Name> --project src/Chores.Api --output-dir Data/Migrations
npm run dev --prefix web                   # Vite dev server on :5173, proxies /api to :5080
npm run build --prefix web                 # front end into src/Chores.Api/wwwroot, served by the API
npm run lint --prefix web
npm test --prefix web                      # Vitest in watch mode; `npm test --prefix web -- --run` runs once
npm run api --prefix web                   # TypeScript types in web/src/api/schema.d.ts from src/Chores.Api/openapi.json
```

Building the API writes its OpenAPI document to `src/Chores.Api/openapi.json`. After changing an endpoint, build, then run `npm run api` and commit both files, so API changes show up as diffs. `npm run api` runs `openapi-typescript` through `npx`, pinned together with TypeScript 5, because it needs TypeScript 5 while the project uses TypeScript 7.

In the front end only `web/src/ui/` imports the UI SDK (`@ui-sdk/*`); screens use those wrappers. The SDK is in beta, so its versions in `package.json` are pinned exactly. Its `Input` doesn't focus on `autoFocus` once the page is up, and a dialog picks its first focus by `[data-autofocus]`: focus a field that appears later from an effect.

In Development the household password is `chores` and the API key is `dev-api-key` (see `src/Chores.Api/appsettings.Development.json`). The login cookie is marked secure, which browsers accept over plain HTTP only on `localhost`.

For front-end work, run the API and the Vite dev server side by side and open port 5173. To see the app the way it ships, build the front end and open port 5080.

The API is published on <http://localhost:5080> and the Vite dev server on <http://localhost:5173>. Local SQLite data lives in `data/` in the workspace (git-ignored), so it survives rebuilding the container. The database is `data/chores.db` relative to the working directory, or the path in the `Database__Path` environment variable; migrations are applied when the API starts.

## Run

The `Dockerfile` builds one image that serves the front end and the API on plain HTTP port 8080; the reverse proxy in front terminates HTTPS and its `X-Forwarded-*` headers are honoured from any address, so only the proxy should reach port 8080. [`compose.yaml`](compose.yaml) is an example for the home server:

```sh
docker compose build
docker compose run --rm chores hash-password   # type the password, prints the hash
# put CHORES_PASSWORD_HASH and CHORES_API_KEY in .env next to compose.yaml
docker compose up -d
```

Data lives in the `chores-data` volume, mounted at `/data` (`Database__Path=/data/chores.db`, cookie keys in `/data/keys`); back that up. The example sets `TZ=Europe/Amsterdam`.

### Configuration

Environment variables, using `__` for nesting:

| Variable | Meaning |
| --- | --- |
| `Auth__PasswordHash` | Hash of the household password (see below). Without it nobody can log in. |
| `Auth__ApiKey` | Key AI tooling sends in the `X-Api-Key` header. Without it no key is accepted. |
| `Database__Path` | SQLite file, default `data/chores.db`. The cookie encryption keys live in `keys/` next to it, so mount that whole directory as a volume, or every restart logs everyone out. |

Generate the password hash, never configure the password itself:

```sh
dotnet run --project src/Chores.Api -- hash-password   # in the devcontainer; type the password, prints the hash
```

Logging in (`POST /api/login`) gives a cookie valid for a year. `/api/health` and the OpenAPI document at `/openapi/v1.json` are public; every other `/api` endpoint needs the cookie or the API key.
