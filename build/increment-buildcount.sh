#!/usr/bin/env bash
set -euo pipefail

COUNTER_FILE="${1:-.build/version/buildcount.txt}"
LOCK_FILE="${COUNTER_FILE}.lock"

mkdir -p "$(dirname "$COUNTER_FILE")"

# Use flock if available (most Linux distros)
if command -v flock >/dev/null 2>&1; then
  exec 9>"$LOCK_FILE"
  flock -x 9

  current=0
  if [[ -f "$COUNTER_FILE" ]]; then
    current="$(tr -d '[:space:]' < "$COUNTER_FILE" || true)"
    [[ -z "$current" ]] && current=0
  fi

  next=$((current + 1))
  echo "$next" > "$COUNTER_FILE"

  echo "$next"
  exit 0
fi

# Fallback (no flock): best-effort increment
current=0
if [[ -f "$COUNTER_FILE" ]]; then
  current="$(tr -d '[:space:]' < "$COUNTER_FILE" || true)"
  [[ -z "$current" ]] && current=0
fi

next=$((current + 1))
echo "$next" > "$COUNTER_FILE"
echo "$next"
