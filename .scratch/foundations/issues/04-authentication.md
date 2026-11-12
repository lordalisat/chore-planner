# Authentication: household password and API key

Status: ready-for-agent
Blocked by: 01

Implement ADR 0001.

## Acceptance

- `POST /api/login` with the household password sets an HTTP-only, secure cookie valid for one year. A wrong password returns 401.
- `POST /api/logout` clears the cookie.
- Requests carrying the configured `X-Api-Key` header are authenticated without a cookie.
- Every other `/api` endpoint returns 401 without a valid cookie or API key. Static front-end files remain public, so the login page can load.
- The password is configured as a hash, never in plain text, and there is a small documented way to generate the hash.
- The front end shows a login page when the API answers 401, and otherwise the placeholder home page.
- Integration tests cover login, logout, the API key, and rejection of anonymous requests.
