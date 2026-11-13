# Solution skeleton

Status: ready-for-agent
Blocked by: 02

Create the solution layout from the spec: `Chores.Domain`, `Chores.Api`, `Chores.Domain.Tests` and a `web/` React + Vite + TypeScript app.

## Acceptance

- `dotnet build` and `dotnet test` succeed from the repo root.
- `Chores.Api` exposes `GET /api/health` and an OpenAPI document.
- `Chores.Api` serves the built `web/` output as static files, with fallback to `index.html` for client routes.
- `web/` is installable as a PWA (manifest, icons, service worker) and shows a placeholder home page.
- In development, the Vite dev server proxies `/api` to the API.
- A `Directory.Build.props` turns on nullable reference types, implicit usings and warnings-as-errors for all projects.
- Everything above works inside the devcontainer from 02.

## Comments

**From 02 (devcontainer):** the container publishes ports 5080 (API) and 5173 (Vite). Make the API listen on `http://0.0.0.0:5080` in its launch profile and Vite on `0.0.0.0:5173` (`server.host: true`), otherwise the host browser can't reach them. Fill in the README "Inside the container" placeholder with the build, test and run commands.
