#!/usr/bin/env bash
set -euo pipefail

# CI verifier for Unity builds
# Downloads artifacts from the latest "Build WebGL (Artifact Only)" run (on main by default),
# finds Editor.log, prints first ERROR context, and sets exit codes for automation:
# 0 = OK/unknown
# 10 = License/activation failure detected
# 20 = Unity version mismatch likely detected
#
# Requirements: GitHub CLI `gh` authenticated with repo access.

WORKFLOW_NAME="${WORKFLOW_NAME:-Build WebGL (Artifact Only)}"
BRANCH="${BRANCH:-main}"
RUN_ID="${1:-}"

tmpdir="$(mktemp -d)"
cleanup() { rm -rf "$tmpdir"; }
trap cleanup EXIT

if ! command -v gh &>/dev/null; then
  echo "gh (GitHub CLI) is required. Install from https://cli.github.com/" >&2
  exit 1
fi

if [[ -z "${RUN_ID}" ]]; then
  echo "Resolving latest run for workflow: $WORKFLOW_NAME on branch $BRANCH ..."
  RUN_ID="$(gh run list --branch "$BRANCH" --workflow "$WORKFLOW_NAME" --limit 1 --json databaseId --jq '.[0].databaseId' || true)"
fi

if [[ -z "${RUN_ID}" || "${RUN_ID}" == "null" ]]; then
  echo "No recent runs found for '$WORKFLOW_NAME' on branch '$BRANCH'." >&2
  exit 0
fi

echo "Using run id: $RUN_ID"
echo "Downloading artifacts..."
gh run download "$RUN_ID" --dir "$tmpdir" || true

# Unzip any zips downloaded
find "$tmpdir" -type f -name "*.zip" -print0 | while IFS= read -r -d '' z; do
  unzip -o "$z" -d "${z%.zip}" >/dev/null 2>&1 || true
done

# Locate Editor.log candidates
mapfile -t logs < <(find "$tmpdir" -type f \( -iname "Editor.log" -o -iname "*editor*.log" \) | sort)
if (( ${#logs[@]} == 0 )); then
  echo "No Editor.log artifacts found; showing first 200 lines of any log file if present."
  anylog="$(find "$tmpdir" -type f -iname "*.log" | head -n1 || true)"
  if [[ -n "$anylog" ]]; then
    head -n 200 "$anylog"
  else
    echo "No logs found in artifacts."
  fi
  exit 0
fi

license_fail=0
version_mismatch=0

print_context() {
  local file="$1"
  echo "========== LOG: $file =========="
  local first_err
  first_err="$(nl -ba "$file" | grep -inm1 -E "error|ERROR|Error" | awk '{print $1}' || true)"
  if [[ -n "$first_err" ]]; then
    local start=$(( first_err > 40 ? first_err-40 : 1 ))
    local end=$(( first_err+40 ))
    sed -n "${start},${end}p" "$file"
  else
    echo "No explicit 'ERROR' line found; showing first 200 lines:"
    head -n 200 "$file"
  fi
}

for f in "${logs[@]}"; do
  print_context "$f"

  # Activation/license signatures
  if grep -qiE "No valid Unity license|ULF parse error|Failed to activate|License is not valid|Entitlement check failed" "$f"; then
    license_fail=1
  fi

  # Version mismatch hints
  if grep -qiE "m_EditorVersion|EditorVersion|Using Unity version|ProjectVersion|requires Unity version" "$f"; then
    version_mismatch=1
  fi
done

if (( license_fail == 1 )); then
  echo "Detected probable Unity license/activation issue."
  exit 10
fi

if (( version_mismatch == 1 )); then
  echo "Detected possible Unity version mismatch between runner and project."
  exit 20
fi

exit 0