#!/usr/bin/env bash
set -euo pipefail
repo_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
source "$repo_dir/scripts/env.sh"
cd "$repo_dir"
exec dotnet run --project src/Sonora.Preview -c Release --urls http://127.0.0.1:5080
