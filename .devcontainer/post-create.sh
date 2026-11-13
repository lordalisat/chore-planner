#!/bin/sh
# Restore dependencies for whatever exists yet; a no-op on an empty repo.
set -e

mkdir -p data

if ls ./*.slnx >/dev/null 2>&1; then
  dotnet restore
fi

if [ -f web/package.json ]; then
  if [ -f web/package-lock.json ]; then npm ci --prefix web; else npm install --prefix web; fi
fi
