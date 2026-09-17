#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"

git diff --check
for forbidden in "connection.json" "pairing.token" "CodexDeck.log" ".DS_Store"; do
  if git ls-files --error-unmatch "$forbidden" >/dev/null 2>&1; then
    echo "Forbidden artifact tracked: $forbidden" >&2
    exit 1
  fi
done
if rg -n --hidden --glob '!**/.git/**' --glob '!**/*.md' --glob '!scripts/github_preflight.sh' 'dev-token|CODEX_DECK_TOKEN=' .; then
  echo "Potential development token found in repository files" >&2
  exit 1
fi
echo "GitHub preflight passed"
