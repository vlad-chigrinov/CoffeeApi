#!/bin/sh
# Runs on container start (nginx image hook): turn env vars into /config.js for the SPA.
set -eu
esc() { printf '%s' "$1" | sed 's/\\/\\\\/g; s/"/\\"/g'; }
cat > /usr/share/nginx/html/config.js <<CONF
window.__CONFIG__ = { primeuiLicense: "$(esc "${PRIMEUI_LICENSE:-}")" }
CONF
