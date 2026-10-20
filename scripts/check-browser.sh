#!/usr/bin/env bash
set -euo pipefail
repo_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
source "$repo_dir/scripts/env.sh"
cd "$repo_dir"
dotnet build src/Sonora.Preview -c Release -m:2
validation_dir=$(mktemp -d /tmp/sonora-browser.XXXXXX)
export SONORA_DATA_FILE="$validation_dir/sales.json"
export SONORA_TEST_URL=http://127.0.0.1:5081
export Logging__LogLevel__Default=Warning
dotnet "$repo_dir/src/Sonora.Preview/bin/Release/net8.0/Sonora.Preview.dll" --contentRoot "$repo_dir/src/Sonora.Preview" --urls "$SONORA_TEST_URL" > "$validation_dir/server.log" 2>&1 &
server_pid=$!
cleanup() { kill "$server_pid" 2>/dev/null || true; wait "$server_pid" 2>/dev/null || true; rm -rf "$validation_dir"; }
trap cleanup EXIT
ready=false
for attempt in {1..50}; do
  if ! kill -0 "$server_pid" 2>/dev/null; then cat "$validation_dir/server.log"; exit 1; fi
  if curl --fail --silent "$SONORA_TEST_URL/api/clock" > /dev/null; then ready=true; break; fi
  sleep 0.2
done
if test "$ready" != true; then cat "$validation_dir/server.log"; exit 1; fi
python3 -u tests/browser_checks.py
