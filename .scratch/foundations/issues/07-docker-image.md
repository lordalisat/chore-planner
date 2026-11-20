# Docker image for the home server

Status: resolved
Blocked by: 03

## Acceptance

- A multi-stage `Dockerfile` builds `web/` with Node, publishes `Chores.Api`, and produces one runtime image that serves both.
- A `compose.yaml` example with a named volume for the SQLite data directory and environment variables for the password hash, API key and `TZ=Europe/Amsterdam`.
- The container listens on plain HTTP; HTTPS is terminated by the existing reverse proxy. Forwarded headers are honoured, so secure cookies work behind the proxy.
- The README "Run" section explains build, run and configuration in a few lines.

## Comments

**From 03 (solution skeleton):** `npm run build --prefix web` writes the front end into `src/Chores.Api/wwwroot` (git-ignored), not `web/dist`. The Node stage therefore has to run before `dotnet publish`, and its output has to land in `src/Chores.Api/wwwroot` in the .NET build stage, so that publish includes it as the web root. The API serves `wwwroot` with fallback to `index.html`. The official ASP.NET runtime image listens on port 8080 by default (`ASPNETCORE_HTTP_PORTS`), while development uses 5080.

**From 05 (persistence):** The database path is `Database:Path` (env `Database__Path`) and defaults to `data/chores.db` relative to the working directory, created on startup with migrations applied. Either mount the volume at `<WORKDIR>/data` or set `Database__Path` to a file on the volume (e.g. `/data/chores.db`). The container user must be able to write to that directory.
