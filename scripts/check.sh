#!/usr/bin/env bash
set -euo pipefail
repo_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
source "$repo_dir/scripts/env.sh"
cd "$repo_dir"
dotnet build Sonora.sln -c Release -m:2
dotnet run --project tests/Sonora.Checks -c Release --no-build
