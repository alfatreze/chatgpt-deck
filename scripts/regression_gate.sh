#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
dotnet_bin="${DOTNET_BIN:-/usr/local/share/dotnet/dotnet}"

"$dotnet_bin" restore CodexDeckPlugin/CodexDeckPlugin.sln
"$dotnet_bin" restore companion/CodexDeck.Companion/CodexDeck.Companion.csproj
"$dotnet_bin" build CodexDeckPlugin/CodexDeckPlugin.sln --no-restore
"$dotnet_bin" build companion/CodexDeck.Companion/CodexDeck.Companion.csproj --no-restore
"$dotnet_bin" run --project shared/CodexDeck.Protocol.Tests/CodexDeck.Protocol.Tests.csproj
python3 shared/validate_fixtures.py
python3 shared/test_protocol.py
echo "Regression gate passed"
