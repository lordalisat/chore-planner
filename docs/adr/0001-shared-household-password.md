# Shared household password, no user accounts

The app is reachable from the internet through a public reverse proxy, so it needs authentication, but nothing in the domain cares who did what. We use a single shared household password (hash in configuration) that issues a long-lived cookie per phone, plus one configured API key sent as a header for AI tooling. There are deliberately no user accounts.

## Considered Options

- **ASP.NET Core Identity with two accounts**: registration, resets and per-user data for a model that never uses "who".
- **Auth at the proxy** (Authelia, basic auth): keeps the app auth-free, but ties it to one proxy setup and plays badly with an installed PWA.
- **Google login with an e-mail allowlist**: no password to manage, but an external dependency and awkward OIDC redirects inside a PWA.

## Consequences

Completions cannot record who did a Chore. Adding that later means introducing accounts, not just a column.
