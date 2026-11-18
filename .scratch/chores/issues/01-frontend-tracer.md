# Frontend tracer: the v1 stack on the existing login

Status: ready-for-agent
Blocked by: —

Prove the whole front-end stack from `spec.md` on what already works (login, logout, session) before any Chore slice depends on it. No new features.

## Scope

- Tailwind v4 via `@tailwindcss/vite`, with theme tokens for `#2f6f4f` and `#f4f1ea`; mobile-first base styles.
- UI SDK (`@ui-sdk/react`, `@ui-sdk/core`) pinned to an exact version. Only `web/src/ui/` imports `@ui-sdk/*`; start with the wrappers this ticket needs (Field, Menu, Toast).
- TanStack Router with code-based routes in `web/src/router.tsx` (no `@tanstack/router-plugin`, no generated route tree): the Overview (`/`) and `/login`; the Overview stays a placeholder. A `beforeLoad` on the authenticated routes reads the session query and redirects to `/login` on 401; logging in returns to where you came from.
- TanStack Query for `/api/session`, login and logout; refetch on window focus on.
- OpenAPI at build time: `Microsoft.Extensions.ApiDescription.Server` writes `openapi.json` into the repo; `npm run api` in `web/` regenerates TS types with `openapi-typescript`; a small typed fetch client uses them. Commit both outputs.
- App shell, responsive and mobile-first (see `spec.md`, Front end): on a phone, a header with a menu (All Chores, hidden until 09; Areas, hidden until 08; Log out) and a floating **+** button (inert until 02). From the `md` breakpoint up, the menu becomes a top navigation bar and **+** becomes a "New Chore" button in it. Content sits in a centered column with a max width.
- Vitest + Testing Library set up, with one test (for example the login error message).

## Acceptance

- Login, wrong password, logout and "server can't be reached" behave as before, now on the new stack, on a phone-width screen and in a desktop browser (about 1280px wide).
- No screen file imports `@ui-sdk/*`; `package.json` has no `^` or `~` for UI SDK packages.
- Changing an API endpoint and building changes `openapi.json`; `npm run api` updates the TS types.
- `npm run build`, `npm run lint` and the Vitest suite pass; the Docker image still builds and serves the app.
- README documents `npm run api` and the test command.
