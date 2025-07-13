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
  # source "$HOME/code/venvs/publish/bin/activate"
  exit 1
fi

# Load secrets
# shellcheck source=/dev/null
. "$KEYPASS_FILE"

########################################
# 2. .NET build pipeline                #
########################################


dotnet clean NetworkMonitorQuantumSecure-Android.csproj

dotnet publish "$HOME/code/NetworkMonitorLib/NetworkMonitor-Maui-Android.csproj" \
  -c Release -r android --self-contained true

cp --no-clobber \
  "$HOME/code/NetworkMonitorLib/bin/Release/net9.0-android/android/NetworkMonitor-Maui-Android.dll" \
  "$HOME/code/NetworkMonitorQuantumSecure/Resources/Raw/dlls/NetworkMonitor.dll"

cp ./Resources/Raw/appsettings-live.json ./Resources/Raw/appsettings.json

dotnet build -c Release -f net9.0-android NetworkMonitorQuantumSecure-Android.csproj

########################################
# 3. Upload to Google Play              #
########################################

python3 publish_to_store.py \
  --service-account-file "$HOME/code/securefiles/gaccount.json" \
  --package-name click.freenetworkmonitor.quantumsecure \
  --aab "$HOME/code/NetworkMonitorQuantumSecure/bin/Release/net9.0-android/click.freenetworkmonitor.quantumsecure-Signed.aab" \
  --track beta

