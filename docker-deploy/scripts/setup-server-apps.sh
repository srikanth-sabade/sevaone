#!/usr/bin/env bash
# Offline-server setup, part 2 (run after setup-server-wso2.sh and after the images are loaded):
# WSO2 IS provisioning -> bff/frontend/admin-panel/nginx -> WSO2 APIM provisioning.
# Usage: ./scripts/setup-server-apps.sh <hostname-or-ip>
set -euo pipefail
cd "$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLIC_HOSTNAME="${1:?usage: $0 <hostname-or-ip>}"
APPS=(docker compose -f docker-compose.apps.server.yml)
ENV_FILE=.env

sed "s/__PUBLIC_HOSTNAME__/${PUBLIC_HOSTNAME}/" bff-config/appsettings.Docker.json.template \
  | sed "s#http://${PUBLIC_HOSTNAME}:5080/api#http://${PUBLIC_HOSTNAME}/api#" > bff-config/appsettings.Docker.json

out="$("${APPS[@]}" run --rm wso2is-provision)"; echo "$out"
get() { printf '%s\n' "$out" | sed -n "s/^$1=//p" | tail -1; }
for pair in CITIZEN:RESULT_CLIENT OPERATOR:RESULT_OPERATOR_CLIENT GOVADMIN:RESULT_GOVADMIN_CLIENT; do
  n="${pair%%:*}"; r="${pair##*:}"
  id="$(get ${r}_ID)"; sec="$(get ${r}_SECRET)"
  [ -n "$id" ] && [ -n "$sec" ] || { echo "provision missing ${n} values" >&2; exit 1; }
  sed -i "s|^WSO2_${n}_ISSUER_CLIENT_ID=.*|WSO2_${n}_ISSUER_CLIENT_ID=${id}|;s|^WSO2_${n}_ISSUER_CLIENT_SECRET=.*|WSO2_${n}_ISSUER_CLIENT_SECRET=${sec}|" "$ENV_FILE"
done

"${APPS[@]}" up -d bff frontend admin-panel nginx
"${APPS[@]}" up --exit-code-from wso2apim-provision wso2apim-provision
echo "==> Portal: http://${PUBLIC_HOSTNAME}/   Admin: http://${PUBLIC_HOSTNAME}/cmdashboard/"
