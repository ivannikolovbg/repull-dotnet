#!/usr/bin/env bash
#
# Re-generate the Kiota client from the Repull OpenAPI spec.
# Snapshots openapi/v1.json then regenerates src/Repull.SDK/.
#
# Usage:
#   scripts/regen.sh                   # uses snapshot at openapi/v1.json
#   scripts/regen.sh --remote          # pulls the latest spec from api.repull.dev first
#
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SPEC="$ROOT/openapi/v1.json"

if [[ "${1:-}" == "--remote" ]]; then
  echo "Pulling latest spec from https://api.repull.dev/openapi.json..."
  curl -fsSL https://api.repull.dev/openapi.json -o "$SPEC"
fi

if [[ ! -f "$SPEC" ]]; then
  echo "Spec not found at $SPEC. Run with --remote to download it."
  exit 1
fi

echo "Regenerating Kiota client into src/Repull.SDK/..."
kiota generate \
  --openapi "$SPEC" \
  --language csharp \
  --class-name RepullClient \
  --namespace-name Repull.SDK \
  --output "$ROOT/src/Repull.SDK" \
  --clean-output

echo "Done. Run 'dotnet build' to verify."
