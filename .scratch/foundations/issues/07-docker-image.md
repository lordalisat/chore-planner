# Docker image for the home server

Status: ready-for-agent
Blocked by: 03

## Acceptance

- A multi-stage `Dockerfile` builds `web/` with Node, publishes `Chores.Api`, and produces one runtime image that serves both.
- A `compose.yaml` example with a named volume for the SQLite data directory and environment variables for the password hash, API key and `TZ=Europe/Amsterdam`.
- The container listens on plain HTTP; HTTPS is terminated by the existing reverse proxy. Forwarded headers are honoured, so secure cookies work behind the proxy.
- The README "Run" section explains build, run and configuration in a few lines.
