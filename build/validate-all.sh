#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

dotnet run --project "$REPO_ROOT/src/Tools/FeTools" -- \
    validate-all \
    --config "$REPO_ROOT/build/validate-all.json" \
    --stop-on-failure \
    "$@"
