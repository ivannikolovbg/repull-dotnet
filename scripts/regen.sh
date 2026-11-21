#!/usr/bin/env bash
#
# Re-generate the Kiota client from the Repull OpenAPI spec.
# Snapshots openapi/v1.json then regenerates src/Repull.SDK/.
#
# Usage:
#   scripts/regen.sh                   # uses snapshot at openapi/v1.json
#   scripts/regen.sh --remote          # pulls the latest spec from api.repull.dev first
#
# ----------------------------------------------------------------------------
# WHY THE BACKUP/RESTORE DANCE BELOW EXISTS
# ----------------------------------------------------------------------------
# Kiota's `--clean-output` flag wipes the entire output directory before
# generating. That nukes hand-maintained files (the .csproj, partial classes,
# extension methods, the factory) that live alongside generated code.
#
# During the v0.1.1 regen we lost 5 hand-maintained files this way and had to
# restore them from HEAD by hand. To make sure that never happens again, we
# snapshot every hand-maintained file to a tempdir BEFORE Kiota runs, and copy
# them back AFTER. The HAND_FILES array is the source of truth — add to it any
# time you introduce a new hand-maintained file under src/Repull.SDK/.
#
# Auto-discovery of `*.Partial.cs` is included as a safety net in case someone
# adds a new partial without remembering to update HAND_FILES.
# ----------------------------------------------------------------------------
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

cd "$ROOT"

# Hand-maintained files that Kiota's --clean-output would wipe.
# Paths are relative to the repo root.
HAND_FILES=(
  "src/Repull.SDK/Repull.SDK.csproj"
  "src/Repull.SDK/RepullClient.Partial.cs"
  "src/Repull.SDK/RepullClientExtensions.cs"
  "src/Repull.SDK/RepullClientFactory.cs"
  "src/Repull.SDK/RepullBackingStore.cs"
  "src/Repull.SDK/RepullJsonParseNodeFactory.cs"
  "src/Repull.SDK/Models/Reservation.Partial.cs"
)

# Auto-discover any other *.Partial.cs files anywhere under src/Repull.SDK/.
# This is a safety net so newly-added partials survive even if HAND_FILES
# wasn't updated.
while IFS= read -r f; do
  case " ${HAND_FILES[*]} " in
    *" $f "*) ;;
    *) HAND_FILES+=("$f");;
  esac
done < <(find src/Repull.SDK -type f -name "*.Partial.cs" 2>/dev/null)

BACKUP_DIR="$(mktemp -d)"
trap 'rm -rf "$BACKUP_DIR"' EXIT

echo "Backing up hand-maintained files to $BACKUP_DIR..."
for f in "${HAND_FILES[@]}"; do
  if [ -f "$f" ]; then
    mkdir -p "$BACKUP_DIR/$(dirname "$f")"
    cp "$f" "$BACKUP_DIR/$f"
    echo "  backed up: $f"
  fi
done

# Kiota generates from a prepared copy of the spec (see scripts/prepare-spec.py):
# object|primitive oneOfs become anyOf so Kiota can read and write the object branch.
GEN_SPEC="$BACKUP_DIR/openapi.kiota.json"
python3 "$ROOT/scripts/prepare-spec.py" "$SPEC" "$GEN_SPEC"

# --backing-store: models record which properties were set, so an explicit null
# (e.g. Vrbo fees `"pets": null`, remove the pet fee) is sent instead of dropped.
echo "Regenerating Kiota client into src/Repull.SDK/..."
kiota generate \
  --openapi "$GEN_SPEC" \
  --language csharp \
  --class-name RepullClient \
  --namespace-name Repull.SDK \
  --output "$ROOT/src/Repull.SDK" \
  --backing-store \
  --clean-output

# Every generated model gets RepullBackingStore (src/Repull.SDK/RepullBackingStore.cs),
# not Kiota's process-wide default: Kiota's store only sends changed values, which
# would drop every field of a model read from JSON. Scoped to this SDK's models, so
# other Kiota clients in the same process are unaffected.
echo "Pointing generated models at RepullBackingStore..."
grep -rl --include='*.cs' 'BackingStoreFactorySingleton.Instance.CreateBackingStore()' "$ROOT/src/Repull.SDK" \
  | xargs -r sed -i 's/BackingStoreFactorySingleton\.Instance\.CreateBackingStore()/global::Repull.SDK.RepullBackingStore.Create()/g'
if grep -rq --include='*.cs' 'BackingStoreFactorySingleton.Instance.CreateBackingStore()' "$ROOT/src/Repull.SDK"; then
  echo "error: a generated model still uses BackingStoreFactorySingleton" >&2
  exit 1
fi
# Kiota's JSON reader skips null properties, so `{"pets": null}` read from JSON would
# lose its null before the write. RepullJsonParseNodeFactory (hand-maintained) keeps them.
echo "Registering RepullJsonParseNodeFactory in RepullClient..."
sed -i 's/ApiClientBuilder\.RegisterDefaultDeserializer<JsonParseNodeFactory>();/global::Repull.SDK.RepullJsonParseNodeFactory.Register();/' "$ROOT/src/Repull.SDK/RepullClient.cs"
grep -q 'RepullJsonParseNodeFactory.Register();' "$ROOT/src/Repull.SDK/RepullClient.cs" || {
  echo "error: could not register RepullJsonParseNodeFactory in RepullClient.cs" >&2
  exit 1
}
# The lock file records the spec Kiota read; point it back at the committed snapshot.
sed -i 's#"descriptionLocation": ".*"#"descriptionLocation": "../../openapi/v1.json"#' "$ROOT/src/Repull.SDK/kiota-lock.json"

echo "Restoring hand-maintained files..."
for f in "${HAND_FILES[@]}"; do
  if [ -f "$BACKUP_DIR/$f" ]; then
    mkdir -p "$(dirname "$f")"
    cp "$BACKUP_DIR/$f" "$f"
    echo "  restored: $f"
  fi
done

echo "Done. Run 'dotnet build' to verify."
