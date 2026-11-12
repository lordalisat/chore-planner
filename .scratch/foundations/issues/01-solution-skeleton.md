# Solution skeleton

Status: ready-for-agent

Create the solution layout from the spec: `Chores.Domain`, `Chores.Api`, `Chores.Domain.Tests` and a `web/` React + Vite + TypeScript app.

## Acceptance

- `dotnet build` and `dotnet test` succeed from the repo root.
- `Chores.Api` exposes `GET /api/health` and an OpenAPI document.
- `Chores.Api` serves the built `web/` output as static files, with fallback to `index.html` for client routes.
- `web/` is installable as a PWA (manifest, icons, service worker) and shows a placeholder home page.
- In development, the Vite dev server proxies `/api` to the API.
