#!/usr/bin/env bash
set -euo pipefail
task_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
mode="${1:-simulator}"
if [ "$#" -gt 0 ]; then shift; fi
if [ "$(uname -s)" != "Darwin" ]; then
  echo "iPhone packaging needs macOS and Xcode." >&2
  exit 1
fi
case "$mode" in
  simulator)
    task_rid="iossimulator-arm64"
    if [ "$(uname -m)" = "x86_64" ]; then task_rid="iossimulator-x64"; fi
    dotnet build "$task_root/src/ToDoOfSorts.App/ToDoOfSorts.App.csproj" -f net10.0-ios -c Debug \
      -p:MobileTarget=net10.0-ios -p:RuntimeIdentifier="$task_rid" -p:EnableCodeSigning=false "$@"
    ;;
  device)
    dotnet build "$task_root/src/ToDoOfSorts.App/ToDoOfSorts.App.csproj" -f net10.0-ios -c Debug \
      -p:MobileTarget=net10.0-ios -p:RuntimeIdentifier=ios-arm64 "$@"
    ;;
  *) echo "Usage: bash scripts/build-ios.sh [simulator|device] [extra MSBuild arguments]" >&2; exit 1 ;;
esac
