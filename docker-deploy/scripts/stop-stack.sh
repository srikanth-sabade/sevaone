#!/usr/bin/env bash
# Safely stops the entire SevaOne stack WITHOUT touching data.
#
# Plain `docker compose down` (no `-v`) never removes volumes -- named or anonymous -- so
# Postgres (sevaone_pgdata: the app's own 'sevaone' db + all 7 WSO2 IS/APIM databases) and the
# AI backend's model cache (sevaone_ai_backend_hf_cache) survive untouched. The shared
# 'sevaone-net' network is also left alone (compose never removes an `external: true` network
# anyway). Re-running ./scripts/setup-stack.sh afterwards just starts everything back up against
# the same data -- no re-provisioning, no re-running Flyway/WSO2 schema load, no new WSO2
# citizen-issuer clientId/secret.
#
# For the destructive full reset (wipes ALL of that data -- only useful when you deliberately
# want to test setup-stack.sh from a clean slate), run this with --wipe. That path requires
# typing "wipe" to confirm, since there is no undo.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${DEPLOY_DIR}"

# Reverse of setup-stack.sh's startup order -- purely for a clean shutdown feel, not a
# correctness requirement (each file's `down` only touches the services defined in it).
COMPOSE_FILES=(
  docker-compose.apps.yml
  docker-compose.wso2apim.yml
  docker-compose.wso2is.yml
  docker-compose.infra.yml
)

WIPE=0
if [ "${1:-}" = "--wipe" ]; then
  WIPE=1
fi

echo "==> Containers currently running for this stack:"
docker ps --filter "label=com.docker.compose.project=sevaone" --format '    {{.Names}} ({{.Status}})'
echo

if [ "${WIPE}" = "1" ]; then
  echo "!! --wipe requested: this PERMANENTLY DELETES all data -- the app's 'sevaone' database,"
  echo "!! all 7 WSO2 IS/APIM databases, and the AI backend's model cache. There is no undo."
  read -r -p "Type 'wipe' to confirm, anything else to abort: " confirm
  if [ "${confirm}" != "wipe" ]; then
    echo "Confirmation not given - aborting. Nothing was touched."
    exit 1
  fi
  echo
fi

for f in "${COMPOSE_FILES[@]}"; do
  echo "==> Stopping ${f} ..."
  if [ "${WIPE}" = "1" ]; then
    docker compose -f "${f}" down -v --remove-orphans
  else
    docker compose -f "${f}" down --remove-orphans
  fi
  echo
done

if [ "${WIPE}" = "1" ]; then
  echo "==> Removing the shared 'sevaone-net' network (setup-stack.sh recreates it automatically) ..."
  docker network rm sevaone-net 2>/dev/null || true
  echo
  echo "==> Full wipe complete. Next ./scripts/setup-stack.sh run starts from true zero."
else
  echo "==> Stack stopped. All data preserved (Postgres, WSO2 IS/APIM state, AI backend model cache)."
  echo "    Run ./scripts/setup-stack.sh again to bring everything back up against the same data."
fi
