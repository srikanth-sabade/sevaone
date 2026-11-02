#!/usr/bin/env bash
# One-click setup for the entire SevaOne stack:
#   1. Ensure the shared 'sevaone-net' external network exists.
#   2. Start postgres/redis/kafka (docker-compose.infra.yml), wait for them to be healthy.
#   3. Run Flyway migrations for the app's own 'sevaone' Postgres database.
#   4. Create the wso2carbon role + all 7 Postgres databases (4 for IS, 3 for APIM),
#      load WSO2's own schema into each.
#   5. Render deployment.toml (IS + APIM) and appsettings.Docker.json (bff) from their
#      templates with the chosen hostname/IP - the one non-secret value all three share.
#   6. Ensure the citizen-issuer signing keypair exists (generate if missing/mismatched),
#      and check docker-deploy/.env has the real external secrets bff needs (fail early with
#      a clear list instead of a cryptic docker-compose error deep into startup).
#   7. Start wso2is, then run wso2is-provision (registers the citizen-issuer trusted token
#      issuer + OAuth app, writes the resulting clientId/clientSecret back into .env -- this
#      MUST happen before bff starts, and after every full reset since it wipes that app too).
#   8. Start wso2apim (its entrypoint waits for IS to import its TLS cert).
#   9. Start the app services from docker-compose.apps.yml: SevaOne bff + frontend +
#      admin-panel (backend, frontend, admin-panel), the nginx gateway in front of them.
#   10. Run wso2apim-provision (Key Manager registration + API import/publish).
#
# NOT started here, on purpose:
#   - legacy apps: the Karnataka dashboard (legacy/dashboard-kar) and legacy/rag-service;
#   - any AI component (ai_backend, sevaone-rag, gov-admin-ai, ai-monitor). They all run on
#     the shared GPU/AI server and are reached over the sevaone-gpu-tunnel.service /
#     sevaone-rag-gpu-tunnel.service SSH tunnels (see docker-deploy/NEW_MACHINE_SETUP.md).
#     The stale docker-compose.ai-backend.yml is never used by this script.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${DEPLOY_DIR}"

INFRA_COMPOSE=(docker compose -f docker-compose.infra.yml)
IS_COMPOSE=(docker compose -f docker-compose.wso2is.yml)
APIM_COMPOSE=(docker compose -f docker-compose.wso2apim.yml)
APPS_COMPOSE=(docker compose -f docker-compose.apps.yml)

ENV_FILE=".env"
[ -f "${ENV_FILE}" ] || touch "${ENV_FILE}"

WSO2_DB_USER="wso2carbon"
WSO2_DB_PASSWORD="wso2carbon"
IS_DATABASES=(wso2_carbon wso2_shared wso2_identity wso2_agent)
APIM_DATABASES=(wso2apim_carbon wso2apim_shared wso2apim_apim)

# --- Step 0: ask for the hostname/IP the whole stack should identify itself as ---------
# Matters once this stack runs on a real server: WSO2 embeds `server.hostname` into every
# issuer/redirect/JWKS URL it generates, and bff's Frontend:PublicOrigin / DigiLocker
# callback URL have the same problem. "localhost" only resolves on the machine itself -
# anyone else hitting this server needs the real reachable hostname or IP instead. One
# prompt, shared by IS, APIM, and bff, since they're all part of one deployment.
echo "The stack needs to know what hostname/IP it's reachable at (WSO2 IS, WSO2 APIM,"
echo "and bff's public-facing URLs all use this)."
echo "  - Running this on your own laptop for local dev? Just press Enter (uses 'localhost')."
echo "  - Running this on a server others will reach? Enter that server's IP or DNS name."
read -r -p "Hostname or IP [localhost]: " PUBLIC_HOSTNAME
PUBLIC_HOSTNAME="${PUBLIC_HOSTNAME:-localhost}"
echo "Using hostname: ${PUBLIC_HOSTNAME}"
echo

# --- Step 1: ensure the shared external network exists ---------------------------------
# docker-compose.infra.yml is the one compose file that actually owns sevaone-net (its
# `networks.default` block, not `external: true`, pins it to 10.99.0.0/24 so it stays off the
# 172.x range Docker's default address pool hands out -- 172.x collided with a VM's own
# network on this host, in practice). Every other compose file just attaches via
# `external: true`, since a network can only be declared/owned in one of several compose
# files that share it.
#
# That means this create MUST use the same subnet as docker-compose.infra.yml's ipam config:
# whichever one runs `up` first actually creates the network, and if this bare `docker network
# create` (no subnet) won that race, Docker would silently hand out a 172.x address instead,
# with docker-compose.infra.yml's own ipam config never taking effect (it only applies when
# compose itself creates the network) -- confirmed to happen in practice on this host.
echo "==> Ensuring the shared 'sevaone-net' network exists (10.99.0.0/24) ..."
docker network inspect sevaone-net >/dev/null 2>&1 || docker network create --subnet=10.99.0.0/24 sevaone-net
echo

# --- Step 2: bring up postgres, redis, kafka and wait for them to be healthy -----------
echo "==> Starting postgres, redis, kafka ..."
"${INFRA_COMPOSE[@]}" up -d --wait
echo "==> Infra is up and healthy."
echo

psql_admin() {
  "${INFRA_COMPOSE[@]}" exec -T postgres psql -v ON_ERROR_STOP=1 -U sevaone "$@"
}
psql_wso2() {
  local db="$1"; shift
  "${INFRA_COMPOSE[@]}" exec -T postgres psql -v ON_ERROR_STOP=1 -U "${WSO2_DB_USER}" -d "${db}" "$@"
}
declare -A DB_CREATED
create_db_if_missing() {
  local db="$1" exists
  exists="$(psql_admin -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='${db}'")"
  if [ "${exists}" != "1" ]; then
    echo "==> Creating database ${db} ..."
    psql_admin -d postgres -c "CREATE DATABASE ${db} OWNER ${WSO2_DB_USER};"
    DB_CREATED["${db}"]=1
  else
    echo "==> Database ${db} already exists, skipping creation."
    DB_CREATED["${db}"]=0
  fi
}
# WSO2's DDL is only partially idempotent: CREATE TABLE/SEQUENCE use IF NOT EXISTS, but
# CREATE INDEX does not - re-running the same file against an already-loaded database fails
# with "relation already exists" (harmless in principle, fatal here since psql runs with
# ON_ERROR_STOP=1). So schema is loaded once, only into databases just created this run -
# not "safe to always re-run" the way the WSO2 IS docs alone would suggest.
load_schema_once() {
  local db="$1" file="$2"
  if [ "${DB_CREATED[${db}]:-0}" = "1" ]; then
    echo "    ${db} <- ${file}"
    psql_wso2 "${db}" < "${file}"
  else
    echo "    ${db} already existed, skipping ${file} (assumed already loaded)"
  fi
}

echo "==> Ensuring Postgres role '${WSO2_DB_USER}' exists ..."
role_exists="$(psql_admin -d postgres -tAc "SELECT 1 FROM pg_roles WHERE rolname='${WSO2_DB_USER}'")"
if [ "${role_exists}" != "1" ]; then
  psql_admin -d postgres -c "CREATE ROLE ${WSO2_DB_USER} LOGIN PASSWORD '${WSO2_DB_PASSWORD}';"
  echo "    created role ${WSO2_DB_USER}"
else
  echo "    role ${WSO2_DB_USER} already exists"
fi
echo

# --- Step 3: run Flyway migrations for the app's own 'sevaone' Postgres database -----
# 'sevaone' db/user/password are created automatically by the postgres image's own
# POSTGRES_DB/POSTGRES_USER/POSTGRES_PASSWORD init (docker-compose.infra.yml) -- only the
# schema migration itself was missing from this script (a real gap hit more than once this
# session: a wiped sevaone_pgdata volume left the app's tables/schemas missing, breaking
# citizen login with "schema portal does not exist" even though everything else was healthy).
echo "==> Running Flyway migrations for the 'sevaone' app database ..."
docker run --rm --network sevaone-net \
  -v "${DEPLOY_DIR}/../backend/database/migrations:/flyway/sql:ro" \
  flyway/flyway:10-alpine \
  -url=jdbc:postgresql://postgres:5432/sevaone -user=sevaone -password=sevaone -connectRetries=10 migrate
echo "==> Flyway migrations applied."
echo

# --- Step 4: WSO2 IS - create its 4 databases, load its schema ------------------------
echo "==> [IS] Creating databases ..."
for db in "${IS_DATABASES[@]}"; do create_db_if_missing "${db}"; done
echo

echo "==> [IS] Loading schema (only into databases just created above) ..."
load_schema_once wso2_carbon   wso2/dbscripts/carbon-and-shared.sql
load_schema_once wso2_shared   wso2/dbscripts/carbon-and-shared.sql
load_schema_once wso2_identity wso2/dbscripts/identity.sql
load_schema_once wso2_identity wso2/dbscripts/consent.sql
load_schema_once wso2_agent    wso2/dbscripts/agent.sql
echo "==> [IS] Schema loaded."
echo

# --- Step 5: WSO2 APIM - create its 3 databases, load its schema ----------------------
echo "==> [APIM] Creating databases ..."
for db in "${APIM_DATABASES[@]}"; do create_db_if_missing "${db}"; done
echo

echo "==> [APIM] Loading schema (only into databases just created above) ..."
load_schema_once wso2apim_carbon wso2-apim/dbscripts/carbon-and-shared.sql
load_schema_once wso2apim_shared wso2-apim/dbscripts/carbon-and-shared.sql
load_schema_once wso2apim_apim   wso2-apim/dbscripts/apim.sql
echo "==> [APIM] Schema loaded."
echo

# --- Step 6: render every *.template file with the chosen hostname --------------------
echo "==> Rendering config templates (hostname = ${PUBLIC_HOSTNAME}) ..."
sed "s/__PUBLIC_HOSTNAME__/${PUBLIC_HOSTNAME}/" \
  wso2/config-volume/repository/conf/deployment.toml.template \
  > wso2/config-volume/repository/conf/deployment.toml
sed "s/__PUBLIC_HOSTNAME__/${PUBLIC_HOSTNAME}/" \
  wso2-apim/config-volume/repository/conf/deployment.toml.template \
  > wso2-apim/config-volume/repository/conf/deployment.toml
sed "s/__PUBLIC_HOSTNAME__/${PUBLIC_HOSTNAME}/" \
  bff-config/appsettings.Docker.json.template \
  > bff-config/appsettings.Docker.json
echo

# --- Step 7a: ensure the citizen-issuer signing keypair exists and matches its cert ----
# The PRIVATE key lives only in .env (gitignored, genuine secret); the PUBLIC cert derived
# from it is committed at wso2/sevaone-citizen-issuer.crt (safe -- it's a public key) and is
# what gets registered as WSO2 IS's trusted-token-issuer certificate in Step 7 below. If
# either is missing, or they don't actually match (confirmed to happen in practice -- e.g.
# the cert got regenerated by hand without updating .env, or vice versa), regenerate both
# together so they're always a consistent pair.
echo "==> Ensuring citizen-issuer signing keypair matches its committed cert ..."
CITIZEN_CERT_FILE="wso2/sevaone-citizen-issuer.crt"

extract_signing_key_pem() {
  awk '
    /^WSO2_CITIZEN_ISSUER_SIGNING_KEY_PEM="/ { flag=1; sub(/^WSO2_CITIZEN_ISSUER_SIGNING_KEY_PEM="/, "") }
    flag { print }
    /-----END PRIVATE KEY-----"$/ { flag=0 }
  ' "${ENV_FILE}" | sed 's/"$//'
}

regenerate_keypair=0
existing_key_pem="$(extract_signing_key_pem)"
if [ -z "${existing_key_pem}" ] || [ ! -f "${CITIZEN_CERT_FILE}" ]; then
  regenerate_keypair=1
else
  key_tmp="$(mktemp)"
  printf '%s\n' "${existing_key_pem}" > "${key_tmp}"
  key_modulus="$(openssl rsa -in "${key_tmp}" -noout -modulus 2>/dev/null | openssl md5)"
  cert_modulus="$(openssl x509 -in "${CITIZEN_CERT_FILE}" -noout -modulus 2>/dev/null | openssl md5)"
  rm -f "${key_tmp}"
  if [ -z "${key_modulus}" ] || [ "${key_modulus}" != "${cert_modulus}" ]; then
    echo "    ${ENV_FILE}'s signing key and ${CITIZEN_CERT_FILE} don't match -- regenerating both."
    regenerate_keypair=1
  fi
fi

if [ "${regenerate_keypair}" = "1" ]; then
  echo "    Generating a fresh RSA keypair for the citizen-issuer trusted token issuer ..."
  key_tmp="$(mktemp)"
  openssl genrsa -out "${key_tmp}" 2048 2>/dev/null
  openssl req -new -x509 -key "${key_tmp}" -out "${CITIZEN_CERT_FILE}" -days 3650 \
    -subj "/CN=sevaone-citizen-issuer/O=SevaOne" 2>/dev/null
  new_key_pem="$(cat "${key_tmp}")"
  rm -f "${key_tmp}"

  tmp_env="$(mktemp)"
  awk '
    /^WSO2_CITIZEN_ISSUER_SIGNING_KEY_PEM="/ { skipping=1 }
    !skipping { print }
    skipping && /-----END PRIVATE KEY-----"$/ { skipping=0 }
  ' "${ENV_FILE}" > "${tmp_env}"
  { cat "${tmp_env}"; echo "WSO2_CITIZEN_ISSUER_SIGNING_KEY_PEM=\"${new_key_pem}\""; } > "${ENV_FILE}"
  rm -f "${tmp_env}"
  echo "    New keypair written: ${CITIZEN_CERT_FILE} (public, safe to commit) + ${ENV_FILE} (private)."
else
  echo "    Existing signing keypair matches ${CITIZEN_CERT_FILE}, reusing."
fi
echo

# --- Step 7b: seed placeholders for the WSO2-generated values so docker-compose can even --
# parse docker-compose.apps.yml before they exist for real -- compose eagerly validates every
# service's ${VAR:?...} interpolation for the whole file, even when only running one unrelated
# service (confirmed empirically), so bff's required env vars must resolve to *something*
# before wso2is-provision (Step 7) can run. wso2is-provision overwrites these with the real
# values before bff ever starts.
for var in WSO2_CITIZEN_ISSUER_CLIENT_ID WSO2_CITIZEN_ISSUER_CLIENT_SECRET \
           WSO2_OPERATOR_ISSUER_CLIENT_ID WSO2_OPERATOR_ISSUER_CLIENT_SECRET \
           WSO2_GOVADMIN_ISSUER_CLIENT_ID WSO2_GOVADMIN_ISSUER_CLIENT_SECRET; do
  grep -q "^${var}=" "${ENV_FILE}" 2>/dev/null || echo "${var}=pending-provisioning" >> "${ENV_FILE}"
done

# --- Step 7c: make sure the real external secrets bff needs are actually set in .env ------
# These can't be generated or templated -- they're genuine external-provider (DigiLocker)
# secrets. Check up front and fail with one clear list, instead of docker compose dying
# confusingly on whichever one it hits first deep into startup.
REQUIRED_VARS=(DIGILOCKER_DEPT_KEY DIGILOCKER_HMAC_KEY)
missing=()
for var in "${REQUIRED_VARS[@]}"; do
  value="$(grep -E "^${var}=" "${ENV_FILE}" 2>/dev/null | tail -1 | cut -d= -f2-)"
  [ -z "${value}" ] && missing+=("${var}")
done
if [ "${#missing[@]}" -gt 0 ]; then
  echo "==> Missing required secrets in ${ENV_FILE} - bff cannot start without these:"
  for var in "${missing[@]}"; do echo "      ${var}"; done
  echo "    Set them in docker-deploy/.env, then re-run this script."
  exit 1
fi
echo "==> All required external secrets present in ${ENV_FILE}."
echo

# --- Step 7: start wso2is, then provision its citizen-issuer app -----------------------
echo "==> Starting wso2is ..."
"${IS_COMPOSE[@]}" up -d
echo

echo "==> Running wso2is-provision (citizen-issuer + operator-issuer + gov-admin-issuer trusted token issuer/OAuth apps) ..."
provision_output="$("${APPS_COMPOSE[@]}" run --rm wso2is-provision)"
echo "${provision_output}"
new_client_id="$(printf '%s\n' "${provision_output}" | sed -n 's/^RESULT_CLIENT_ID=//p' | tail -1)"
new_client_secret="$(printf '%s\n' "${provision_output}" | sed -n 's/^RESULT_CLIENT_SECRET=//p' | tail -1)"
new_operator_client_id="$(printf '%s\n' "${provision_output}" | sed -n 's/^RESULT_OPERATOR_CLIENT_ID=//p' | tail -1)"
new_operator_client_secret="$(printf '%s\n' "${provision_output}" | sed -n 's/^RESULT_OPERATOR_CLIENT_SECRET=//p' | tail -1)"
new_govadmin_client_id="$(printf '%s\n' "${provision_output}" | sed -n 's/^RESULT_GOVADMIN_CLIENT_ID=//p' | tail -1)"
new_govadmin_client_secret="$(printf '%s\n' "${provision_output}" | sed -n 's/^RESULT_GOVADMIN_CLIENT_SECRET=//p' | tail -1)"
if [ -z "${new_client_id}" ] || [ -z "${new_client_secret}" ] \
   || [ -z "${new_operator_client_id}" ] || [ -z "${new_operator_client_secret}" ] \
   || [ -z "${new_govadmin_client_id}" ] || [ -z "${new_govadmin_client_secret}" ]; then
  echo "==> wso2is-provision did not report every expected clientId/clientSecret - aborting." >&2
  exit 1
fi
sed -i "s|^WSO2_CITIZEN_ISSUER_CLIENT_ID=.*|WSO2_CITIZEN_ISSUER_CLIENT_ID=${new_client_id}|" "${ENV_FILE}"
sed -i "s|^WSO2_CITIZEN_ISSUER_CLIENT_SECRET=.*|WSO2_CITIZEN_ISSUER_CLIENT_SECRET=${new_client_secret}|" "${ENV_FILE}"
sed -i "s|^WSO2_OPERATOR_ISSUER_CLIENT_ID=.*|WSO2_OPERATOR_ISSUER_CLIENT_ID=${new_operator_client_id}|" "${ENV_FILE}"
sed -i "s|^WSO2_OPERATOR_ISSUER_CLIENT_SECRET=.*|WSO2_OPERATOR_ISSUER_CLIENT_SECRET=${new_operator_client_secret}|" "${ENV_FILE}"
sed -i "s|^WSO2_GOVADMIN_ISSUER_CLIENT_ID=.*|WSO2_GOVADMIN_ISSUER_CLIENT_ID=${new_govadmin_client_id}|" "${ENV_FILE}"
sed -i "s|^WSO2_GOVADMIN_ISSUER_CLIENT_SECRET=.*|WSO2_GOVADMIN_ISSUER_CLIENT_SECRET=${new_govadmin_client_secret}|" "${ENV_FILE}"
echo "==> ${ENV_FILE} updated with the current WSO2 citizen-issuer, operator-issuer, and gov-admin-issuer clientId/secret."
echo

# --- Step 8: start wso2apim -------------------------------------------------------------
echo "==> Starting wso2apim (its entrypoint waits for wso2is to become reachable to import its TLS cert) ..."
"${APIM_COMPOSE[@]}" up -d
echo

# --- Step 9: start the app services: SevaOne bff + frontend + admin-panel (backend, frontend, admin-panel),
# and the nginx gateway in front of them. Deliberately
# NOT started: legacy apps (dashboard-kar, rag-service) and every AI component (see header). ---
echo "==> Starting bff, frontend, nginx, admin-panel ..."
"${APPS_COMPOSE[@]}" up -d --build bff frontend nginx admin-panel
echo

# --- Step 10: provision wso2apim (Key Manager registration + API import/publish) -------
# provision.sh itself now retries every WSO2 REST call until APIM's management APIs are
# genuinely ready (the login page it used to treat as "ready" responds ~30s into boot, but
# the actual publisher/admin/DCR endpoints can stay flaky for a couple more minutes) and
# aborts with a clear message instead of silently continuing past a failed step.
#
# But `docker compose up <service>` (no -d) always exits 0 once that container stops,
# regardless of the container's own exit code -- confirmed empirically, not just docs. Without
# --exit-code-from, a provisioning failure here would print "Done." below as if nothing had
# gone wrong, exactly the silent-failure this whole step is meant to avoid.
echo "==> Running wso2apim-provision (Key Manager registration + API import/publish) ..."
"${APPS_COMPOSE[@]}" up --exit-code-from wso2apim-provision wso2apim-provision
echo

echo "==> Done."
echo "    WSO2 IS console:          https://${PUBLIC_HOSTNAME}:9443/carbon/admin/login.jsp (admin/admin)"
echo "    APIM Publisher/Devportal: https://${PUBLIC_HOSTNAME}:9444/publisher (admin/admin)"
echo "    SevaOne portal:           http://${PUBLIC_HOSTNAME}"
echo "    Admin panel:              http://${PUBLIC_HOSTNAME}:5096"
echo
echo "    AI (chat, admin AI status) is NOT started by this script -- it's reached over the"
echo "    GPU-box SSH tunnels. See docker-deploy/NEW_MACHINE_SETUP.md if those aren't set up yet."
echo
echo "    Tail logs:"
echo "      docker compose -f docker-compose.wso2is.yml logs -f wso2is"
echo "      docker compose -f docker-compose.wso2apim.yml logs -f wso2apim"
echo "      docker compose -f docker-compose.apps.yml logs -f bff"
