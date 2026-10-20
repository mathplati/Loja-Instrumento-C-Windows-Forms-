#!/usr/bin/env bash
# Use com source; mantém o SDK padrão de outras máquinas quando não há SDK local.
if test -x /workspace/.dotnet/dotnet; then
  export DOTNET_ROOT=/workspace/.dotnet
  export PATH="/workspace/.dotnet:$PATH"
  export DOTNET_CLI_HOME=/workspace/.dotnet-cli
  export NUGET_PACKAGES=/workspace/.nuget/packages
  export XDG_RUNTIME_DIR=/workspace/.dotnet-runtime
  export XDG_DATA_HOME=/workspace/.local/share
  mkdir -p "$XDG_RUNTIME_DIR" "$XDG_DATA_HOME"
  chmod 700 "$XDG_RUNTIME_DIR"
fi
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
