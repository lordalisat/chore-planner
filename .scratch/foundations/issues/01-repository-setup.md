# Repository setup

Status: ready-for-agent

The housekeeping files every later issue relies on, added before any code.

## Acceptance

- `README.md`: what the app is (one paragraph), links to `BRIEF.md`, `GLOSSARY.md`, `docs/adr/` and `.scratch/`, and placeholder sections for "Develop" (filled by 02/03) and "Run" (filled by 07).
- `.gitignore` covering .NET (`bin/`, `obj/`, `*.user`), Node (`node_modules/`, `web/dist/`), IDE folders (`.vs/`, `.idea/`, but not `.vscode/` files that are meant to be shared), local SQLite data (`*.db`, `*.db-shm`, `*.db-wal`, `data/`), `.env`, and `.claude/worktrees/`. `.scratch/` stays tracked.
- `.gitattributes` normalising line endings (`* text=auto eol=lf`).
- `.editorconfig` with C# conventions (4 spaces, file-scoped namespaces, `var` where the type is apparent) and TypeScript/JSON/YAML (2 spaces).
- `global.json` pinning the .NET 10 SDK (latest feature band, roll forward on patch).
