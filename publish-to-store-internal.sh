#!/usr/bin/env bash
# publish_and_build.sh
set -euo pipefail

########################################
# 1. Preconditions                      #
########################################

# Ensure keypass exists
KEYPASS_FILE="$HOME/code/securefiles/keypass"
if [[ ! -f "$KEYPASS_FILE" ]]; then
  echo "❌ keypass file not found at $KEYPASS_FILE" >&2
  exit 1
fi

# Ensure we’re running inside a Python virtual environment
if [[ -z "${VIRTUAL_ENV:-}" ]]; then
  echo "❌ No Python virtual environment is active."
  echo "   Run:  source /path/to/venv/bin/activate"
  # If you prefer auto-activation, replace the two lines above with:
  # source "venv/bin/activate"
  exit 1
fi

# Load secrets
# shellcheck source=/dev/null
. "$KEYPASS_FILE"

########################################
# 2. .NET build pipeline                #
########################################

dotnet clean NetworkMonitorQuantumSecure-Android.csproj

# Build and stage DLLs (run in its own directory so script_debug.log is in the right place)
MAKE_DIR="$HOME/code/NetworkMonitorLib"
if ! ( cd "$MAKE_DIR" && ./make-dlls ); then
  echo "make-dlls failed — check $MAKE_DIR/script_debug.log" >&2
  exit 1
fi

cp ./Resources/Raw/appsettings-live.json ./Resources/Raw/appsettings.json

dotnet build -c Release -f net10.0-android NetworkMonitorQuantumSecure-Android.csproj

########################################
# 3. Upload to Google Play              #
########################################

python3 publish_to_store.py \
  --service-account-file "$HOME/code/securefiles/gaccount.json" \
  --package-name click.freenetworkmonitor.quantumsecure \
  --aab "$HOME/code/NetworkMonitorQuantumSecure/bin/Release/net10.0-android/click.freenetworkmonitor.quantumsecure-Signed.aab" \
  --track internal

