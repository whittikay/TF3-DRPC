#!/usr/bin/env bash
# Run workload-free Linux projects with isolated, per-worktree build caches.
set -euo pipefail

if [[ "$(uname -s)" != Linux ]]; then
    printf '%s\n' 'This entry point is for Linux only.' >&2
    exit 2
fi

repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
common_git="$(git -C "$repo" rev-parse --path-format=absolute --git-common-dir)"
primary="$(dirname -- "$common_git")"
sdk_root="${TF3DRPC_DOTNET_ROOT:-}"
if [[ -z "$sdk_root" && -x "$repo/.tools/dotnet/dotnet" ]]; then
    sdk_root="$repo/.tools/dotnet"
elif [[ -z "$sdk_root" && -x "$primary/.tools/dotnet/dotnet" ]]; then
    sdk_root="$primary/.tools/dotnet"
fi

if [[ -n "$sdk_root" ]]; then
    sdk_root="$(cd -- "$sdk_root" && pwd)"
    dotnet="$sdk_root/dotnet"
    export DOTNET_ROOT="$sdk_root"
else
    dotnet="$(command -v dotnet)" || {
        printf '%s\n' 'Install the pinned SDK locally or set TF3DRPC_DOTNET_ROOT.' >&2
        exit 2
    }
fi

export DOTNET_CLI_HOME="$repo/.local-build/dotnet-home"
export NUGET_PACKAGES="$repo/.local-build/nuget"
export NUGET_HTTP_CACHE_PATH="$repo/.local-build/nuget-http"
export TMPDIR="$repo/.local-build/tmp"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_NOLOGO=1
export DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true
# These projects use no workloads. Keep global.json's MAUI workload pin untouched.
export MSBuildEnableWorkloadResolver=false
# This wrapper must never opt into publishing a real Discord activity.
unset TF3DRPC_DISCORD_LIVE
mkdir -p -- "$DOTNET_CLI_HOME" "$NUGET_PACKAGES" "$NUGET_HTTP_CACHE_PATH" "$TMPDIR"
cd -- "$repo/companion"
exec "$dotnet" "$@"
