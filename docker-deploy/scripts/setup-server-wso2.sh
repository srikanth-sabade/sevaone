#!/usr/bin/env bash
# Offline-server setup, part 1: Postgres/Redis/Kafka (internal only), the app database,
# WSO2 IS (9443) and WSO2 APIM (9444 / gateway 8244). No image is pulled or built here.
# Usage: ./scripts/setup-server-wso2.sh <hostname-or-ip>
set -euo pipefail
cd "$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLIC_HOSTNAME="${1:?usage: $0 <hostname-or-ip>}"

INFRA=(docker compose -f docker-compose.infra.server.yml)
IS=(docker compose -f docker-compose.wso2is.yml)
APIM=(docker compose -f docker-compose.wso2apim.yml)
DB_USER=wso2carbon

docker network inspect sevaone-net >/dev/null 2>&1 || docker network create --subnet=10.99.0.0/24 sevaone-net

"${INFRA[@]}" up -d --wait

psql_admin() { "${INFRA[@]}" exec -T postgres psql -v ON_ERROR_STOP=1 -U sevaone "$@"; }
psql_wso2()  { local db="$1"; shift; "${INFRA[@]}" exec -T postgres psql -v ON_ERROR_STOP=1 -U "$DB_USER" -d "$db" "$@"; }
declare -A CREATED
mkdb() {
  if [ "$(psql_admin -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$1'")" != 1 ]; then
    psql_admin -d postgres -c "CREATE DATABASE $1 OWNER $DB_USER;"; CREATED[$1]=1
  else CREATED[$1]=0; fi
}
load() { [ "${CREATED[$1]}" = 1 ] && { echo "    $1 <- $2"; psql_wso2 "$1" < "$2"; } || echo "    $1 exists, skipping $2"; }

[ "$(psql_admin -d postgres -tAc "SELECT 1 FROM pg_roles WHERE rolname='$DB_USER'")" = 1 ] \
  || psql_admin -d postgres -c "CREATE ROLE $DB_USER LOGIN PASSWORD 'wso2carbon';"

echo "==> Flyway (app database)"
docker run --rm --network sevaone-net -v "$PWD/../backend/database/migrations:/flyway/sql:ro" \
  flyway/flyway:10-alpine -url=jdbc:postgresql://postgres:5432/sevaone -user=sevaone -password=sevaone -connectRetries=10 migrate

for db in wso2_carbon wso2_shared wso2_identity wso2_agent wso2apim_carbon wso2apim_shared wso2apim_apim; do mkdb "$db"; done
load wso2_carbon   wso2/dbscripts/carbon-and-shared.sql
load wso2_shared   wso2/dbscripts/carbon-and-shared.sql
load wso2_identity wso2/dbscripts/identity.sql
load wso2_identity wso2/dbscripts/consent.sql
load wso2_agent    wso2/dbscripts/agent.sql
load wso2apim_carbon wso2-apim/dbscripts/carbon-and-shared.sql
load wso2apim_shared wso2-apim/dbscripts/carbon-and-shared.sql
load wso2apim_apim   wso2-apim/dbscripts/apim.sql

sed "s/__PUBLIC_HOSTNAME__/${PUBLIC_HOSTNAME}/" wso2/config-volume/repository/conf/deployment.toml.template > wso2/config-volume/repository/conf/deployment.toml
sed "s/__PUBLIC_HOSTNAME__/${PUBLIC_HOSTNAME}/" wso2-apim/config-volume/repository/conf/deployment.toml.template > wso2-apim/config-volume/repository/conf/deployment.toml

"${IS[@]}" up -d
"${APIM[@]}" up -d
echo "==> Started. IS: https://${PUBLIC_HOSTNAME}:9443/carbon   APIM: https://${PUBLIC_HOSTNAME}:9444/publisher"
