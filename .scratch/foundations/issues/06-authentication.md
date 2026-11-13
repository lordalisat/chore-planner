# Authentication: household password and API key

Status: ready-for-agent
Blocked by: 03

Implement ADR 0001.

## Acceptance

- `POST /api/login` with the household password sets an HTTP-only, secure cookie valid for one year. A wrong password returns 401.
- `POST /api/logout` clears the cookie.
- Requests carrying the configured `X-Api-Key` header are authenticated without a cookie.
- Every other `/api` endpoint returns 401 without a valid cookie or API key. Static front-end files remain public, so the login page can load.
- The password is configured as a hash, never in plain text, and there is a small documented way to generate the hash.
- The front end shows a login page when the API answers 401, and otherwise the placeholder home page.
- Integration tests cover login, logout, the API key, and rejection of anonymous requests.

## Comments

**From 03 (solution skeleton):**
- `Program.cs` maps an `/api` group fallback that returns 404 for unknown API paths, so they never fall through to `index.html`. Make sure that fallback does not answer anonymous requests with 404 where this ticket requires 401; put the authorization requirement on the whole `/api` group, fallback included.
- `/openapi/v1.json` is mapped outside `/api` and is public in every environment. Decide whether it stays anonymous (it is how AI tooling discovers the API) or requires the cookie or API key.
- `/api/health` exists. Decide whether it stays anonymous, for example for a Docker health check in 07.
- `tests/Chores.Api.Tests` already has an `ApiFactory` (`WebApplicationFactory<Program>`, `Testing` environment, temporary web root) to build the integration tests on.
