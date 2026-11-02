#!/bin/sh
# Runs once after wso2apim (and bff) are up. Registers WSO2 IS as APIM's
# external Key Manager (via the keymanager-operations connector shim baked
# into the wso2is image -- see Docker-deploy/wso2/artifact-volume/ and
# backend/docs/WSO2_IS_APIM_SETUP.md for why that's required), then imports,
# publishes and deploys two API Products from SevaOne's live OpenAPI spec:
# SevaOne-API (department, full spec -- dept/* operations get the same gateway-secret
# header injection pattern, see "department gateway-secret stamp" below) and
# SevaOne-Citizen-API (citizen-facing paths only, authType None + gateway-secret header
# injection on every operation -- see backend/docs/WSO2_IS_APIM_SETUP.md, "Citizen authentication").
#
# Needs curl + jq + python3 on PATH (the wso2apim-provision service installs
# all three via apk before running this -- see docker-compose.yml).
set -eu

APIM_URL="${APIM_URL:-https://wso2apim:9443}"
IS_URL="${IS_URL:-https://wso2is:9443}"
BFF_URL="${BFF_URL:-http://bff:8080}"
ADMIN_USER="${ADMIN_USER:-admin}"
ADMIN_PASS="${ADMIN_PASS:-admin}"
GATEWAY_VHOST="${GATEWAY_VHOST:-localhost}"
CITIZEN_GATEWAY_SECRET="${CITIZEN_GATEWAY_SECRET:?CITIZEN_GATEWAY_SECRET must be set -- must match bff CitizenGatewayAuth__Secret}"
DEPT_GATEWAY_SECRET="${DEPT_GATEWAY_SECRET:?DEPT_GATEWAY_SECRET must be set -- must match bff DeptGatewayAuth__Secret}"

echo "Waiting for WSO2 APIM at ${APIM_URL} ..."
i=0
until curl -skf "${APIM_URL}/services/Version" >/dev/null 2>&1 || curl -sk "${APIM_URL}/carbon/admin/login.jsp" >/dev/null 2>&1; do
  i=$((i + 1))
  if [ "${i}" -ge 90 ]; then echo "APIM did not come up in time" >&2; exit 1; fi
  echo "    still waiting for WSO2 APIM ... (attempt ${i}/90, $((i * 5))s elapsed)"
  sleep 5
done
echo "APIM reachable (attempt ${i})."

# The login page above answers as soon as the HTTPS listener opens, but APIM's actual
# management REST APIs (client-registration, oauth2/token, publisher/admin v4) are separate
# webapps whose OSGi components can still be activating for another 1-3 minutes after that --
# confirmed in practice: DCR + token succeeded here while the very next call (key-managers
# list) came back empty, followed by "Key Manager Already Exists" on create, then API imports
# that "succeeded" with id=null. curl exit codes and json values were never checked below, so
# the script sailed through all of that and reported success with nothing actually created.
#
# http_json retries any single call here until it gets a 2xx, and require_nonempty aborts
# loudly instead of continuing with a null id -- together they make every step below wait out
# APIM's real startup instead of racing it.
MAX_HTTP_RETRIES=48  # 48 * 5s = 4 minutes per call
http_json() {
  local desc="$1"; shift
  local attempt=0 resp status body
  while true; do
    attempt=$((attempt + 1))
    resp="$(curl -sk -w '\n%{http_code}' "$@")"
    status="${resp##*$'\n'}"
    body="${resp%$'\n'*}"
    case "${status}" in
      2??) echo "${body}"; return 0 ;;
    esac
    if [ "${attempt}" -ge "${MAX_HTTP_RETRIES}" ]; then
      echo "==> ${desc} failed after ${attempt} attempts (last HTTP ${status}): ${body}" >&2
      exit 1
    fi
    echo "    ${desc}: HTTP ${status}, APIM likely still finishing startup -- retrying (attempt ${attempt}/${MAX_HTTP_RETRIES}) ..." >&2
    sleep 5
  done
}
require_nonempty() {
  local desc="$1" value="$2"
  if [ -z "${value}" ] || [ "${value}" = "null" ]; then
    echo "==> ${desc} was empty/null - aborting." >&2
    exit 1
  fi
}

# WSO2 APIM enforces a hard cap of 5 revisions per API - confirmed live, a real run of this
# script hit "Maximum number of revisions per API has reached" on SevaOne-API's
# operator-authtype revision creation below, because every re-run whose "dirty" check trips
# creates a new revision (4 separate call sites for SevaOne-API alone, 1 for
# SevaOne-Citizen-API) and none of them ever cleaned up the old ones. Confirmed live via the
# Publisher API that only the MOST RECENT revision is actually deployed (non-empty
# deploymentInfo) in this single-environment ("Default") setup - every older one is pure
# history with nothing depending on it, so deleting them is always safe. Called before every
# revision-creation call below, not just the one that failed, since all five call sites
# contribute to the same cap.
MAX_REVISIONS=5
prune_old_revisions() {
  local api_id="$1" revisions count undeployed_ids rev_id
  revisions="$(http_json "Revisions list (${api_id})" -H "Authorization: Bearer ${token}" "${APIM_URL}/api/am/publisher/v4/apis/${api_id}/revisions")"
  count="$(echo "${revisions}" | jq -r '.count')"
  if [ "${count}" -lt "${MAX_REVISIONS}" ]; then
    return 0
  fi
  undeployed_ids="$(echo "${revisions}" | jq -r '.list[] | select((.deploymentInfo // []) | length == 0) | .id')"
  if [ -z "${undeployed_ids}" ]; then
    echo "==> API ${api_id} has ${count} revisions and every one is deployed - cannot prune automatically (unexpected in this single-environment setup)." >&2
    exit 1
  fi
  for rev_id in ${undeployed_ids}; do
    echo "    At the revision cap (${count}/${MAX_REVISIONS}) - pruning old undeployed revision ${rev_id} on API ${api_id} ..." >&2
    curl -sk -o /dev/null -X DELETE -H "Authorization: Bearer ${token}" "${APIM_URL}/api/am/publisher/v4/apis/${api_id}/revisions/${rev_id}"
  done
}

# Confirmed by direct DB inspection (twice, on two separate container recreations): WSO2 APIM's
# local search/registry index does NOT survive the wso2apim container being recreated, but its
# Postgres-backed AM_API table does. So a previously-published API can vanish from Publisher/
# DevPortal search (making it look like nothing was ever provisioned) while still holding its
# context, which makes a fresh import fail with a permanent 409 -- more retries never fix this,
# since it isn't transient. import_api_with_self_heal below detects exactly that combination
# (409, no matching API found in the DB either -> real conflict; 409 WITH a matching row ->
# orphaned by the index loss) and recovers by deleting the orphaned row via WSO2's own REST
# DELETE (not raw SQL -- that endpoint's cascade logic correctly cleans up the ~20 related
# AM_* tables) before retrying the import, instead of retrying a request that can never succeed.
# Status update: the index now lives in the named volume wso2apim_solr (docker-compose.wso2apim.yml),
# so recreating the container no longer loses it and this orphan state should not arise. Two cases
# remain. (1) The volume is removed on its own while the database is kept. (2) The API has
# subscriptions: the REST DELETE below is then refused with 409 ("active subscriptions exist"), so the
# self-heal cannot help. Both are fixed the same way, without losing data: stop wso2apim, delete the
# 'lastaccesstime' row from wso2apim_carbon (reg_resource, path
# /_system/local/repository/components/org.wso2.carbon.registry/indexing), start wso2apim, wait for
# its index to rebuild, then re-run this script.
PGHOST="${PGHOST:-postgres}"
PGPORT="${PGPORT:-5432}"
PGUSER="${PGUSER:-wso2carbon}"
PGPASSWORD="${PGPASSWORD:-wso2carbon}"
PGDATABASE="${PGDATABASE:-wso2apim_apim}"
export PGPASSWORD

import_api_with_self_heal() {
  # $1=desc  $2=full context including version (e.g. /sevaone/1.0.0)  $3..=curl args for the import call
  local desc="$1" full_context="$2"; shift 2
  local healed=0 attempt=0 resp status body stale_uuid del_status
  while true; do
    attempt=$((attempt + 1))
    resp="$(curl -sk -w '\n%{http_code}' "$@")"
    status="${resp##*$'\n'}"
    body="${resp%$'\n'*}"
    case "${status}" in
      2??) echo "${body}"; return 0 ;;
      409)
        if [ "${healed}" -eq 0 ]; then
          stale_uuid="$(psql -h "${PGHOST}" -p "${PGPORT}" -U "${PGUSER}" -d "${PGDATABASE}" -tAc \
            "SELECT api_uuid FROM am_api WHERE context = '${full_context}' LIMIT 1" 2>/dev/null | tr -d '[:space:]')"
          if [ -n "${stale_uuid}" ]; then
            echo "    ${desc}: context ${full_context} already exists in Postgres (uuid=${stale_uuid}) but wasn't found by the existence check above -- this is the known index-loss orphan, not a real conflict. Deleting it via REST and retrying the import ..." >&2
            del_status="$(curl -sk -o /dev/null -w '%{http_code}' -X DELETE -H "Authorization: Bearer ${token}" "${APIM_URL}/api/am/publisher/v4/apis/${stale_uuid}")"
            if [ "${del_status}" = "200" ]; then
              healed=1
              continue
            fi
            echo "    Cleanup DELETE returned HTTP ${del_status} -- falling back to normal retry." >&2
          fi
        fi
        ;;
    esac
    if [ "${attempt}" -ge "${MAX_HTTP_RETRIES}" ]; then
      echo "==> ${desc} failed after ${attempt} attempts (last HTTP ${status}): ${body}" >&2
      exit 1
    fi
    echo "    ${desc}: HTTP ${status}, retrying (attempt ${attempt}/${MAX_HTTP_RETRIES}) ..." >&2
    sleep 5
  done
}

echo "Waiting for SevaOne backend (bff) at ${BFF_URL} ..."
i=0
until curl -sf "${BFF_URL}/api/v1/health" >/dev/null 2>&1; do
  i=$((i + 1))
  if [ "${i}" -ge 60 ]; then echo "bff did not come up in time" >&2; exit 1; fi
  echo "    still waiting for bff ... (attempt ${i}/60, $((i * 5))s elapsed)"
  sleep 5
done
echo "bff reachable."

echo "Registering admin scripting client via DCR ..."
dcr_json="$(http_json "DCR registration" -u "${ADMIN_USER}:${ADMIN_PASS}" -H 'Content-Type: application/json' \
  "${APIM_URL}/client-registration/v0.17/register" -d '{
    "callbackUrl": "",
    "clientName": "seva_apim_provision",
    "owner": "'"${ADMIN_USER}"'",
    "grantType": "password client_credentials refresh_token",
    "saasApp": true
  }')"
client_id="$(echo "${dcr_json}" | jq -r '.clientId')"
client_secret="$(echo "${dcr_json}" | jq -r '.clientSecret')"
require_nonempty "DCR clientId" "${client_id}"
require_nonempty "DCR clientSecret" "${client_secret}"
echo "DCR client: ${client_id}"

echo "Getting admin-scoped access token ..."
token_json="$(http_json "OAuth token request" -u "${client_id}:${client_secret}" \
  -d "grant_type=password&username=${ADMIN_USER}&password=${ADMIN_PASS}&scope=apim:admin apim:api_create apim:api_publish apim:api_view apim:api_manage apim:subscribe apim:common_operation_policy_view service_catalog:service_view service_catalog:service_write" \
  "${APIM_URL}/oauth2/token")"
token="$(echo "${token_json}" | jq -r '.access_token')"
require_nonempty "OAuth access_token" "${token}"

echo "Checking whether WSO2-IS-KM key manager already exists ..."
existing_km_id="$(http_json "Key manager list" -H "Authorization: Bearer ${token}" "${APIM_URL}/api/am/admin/v4/key-managers" \
  | jq -r '[.list[]? | select(.name=="WSO2-IS-KM")][0].id // empty')"

if [ -z "${existing_km_id}" ]; then
  echo "Registering WSO2 IS as external Key Manager ..."
  # 409 here means the list check above raced a KM that was actually already there (seen in
  # practice right after startup, before the admin API's view is fully consistent) -- that's
  # fine, not a failure. Anything else (5xx while APIM is still warming up, connection resets)
  # gets the same retry-until-2xx treatment as every other call below.
  km_attempt=0
  while true; do
    km_attempt=$((km_attempt + 1))
    km_resp="$(curl -sk -w '\n%{http_code}' -X POST -H "Authorization: Bearer ${token}" -H 'Content-Type: application/json' \
    "${APIM_URL}/api/am/admin/v4/key-managers" -d '{
      "name": "WSO2-IS-KM",
      "displayName": "WSO2 Identity Server",
      "type": "WSO2-IS",
      "description": "Dockerized WSO2 Identity Server used as the Key Manager for SevaOne",
      "enabled": true,
      "issuer": "'"${IS_URL}"'/oauth2/token",
      "authorizeEndpoint": "'"${IS_URL}"'/oauth2/authorize",
      "tokenEndpoint": "'"${IS_URL}"'/oauth2/token",
      "revokeEndpoint": "'"${IS_URL}"'/oauth2/revoke",
      "userInfoEndpoint": "'"${IS_URL}"'/keymanager-operations/user-info/claims/generate",
      "introspectionEndpoint": "'"${IS_URL}"'/oauth2/introspect",
      "clientRegistrationEndpoint": "'"${IS_URL}"'/keymanager-operations/dcr/register",
      "wellKnownEndpoint": "'"${IS_URL}"'/oauth2/token/.well-known/openid-configuration",
      "scopeManagementEndpoint": "'"${IS_URL}"'/api/identity/oauth2/v1.0/scopes",
      "availableGrantTypes": ["client_credentials", "password", "authorization_code", "refresh_token"],
      "enableTokenGeneration": true,
      "enableOAuthAppCreation": true,
      "enableMapOAuthConsumerApps": true,
      "additionalProperties": {
        "Username": "'"${ADMIN_USER}"'",
        "Password": "'"${ADMIN_PASS}"'",
        "self_validate_jwt": "false",
        "Certificate_type": "JWKS",
        "certificate_value": "'"${IS_URL}"'/oauth2/jwks"
      }
    }')"
    km_status="${km_resp##*$'\n'}"
    km_body="${km_resp%$'\n'*}"
    case "${km_status}" in
      2??)
        echo "Key manager registered. Restart the wso2apim container once for this to take"
        echo "full effect (APIM's deployment-sync events don't reliably reach the running"
        echo "gateway -- see backend/docs/WSO2_IS_APIM_SETUP.md, Gotcha 2)."
        break
        ;;
      409)
        echo "WSO2-IS-KM already existed (raced the list check above) - treating as registered."
        break
        ;;
      *)
        if [ "${km_attempt}" -ge "${MAX_HTTP_RETRIES}" ]; then
          echo "==> Key manager creation failed after ${km_attempt} attempts (last HTTP ${km_status}): ${km_body}" >&2
          exit 1
        fi
        echo "    Key manager creation: HTTP ${km_status}, APIM likely still finishing startup -- retrying (attempt ${km_attempt}/${MAX_HTTP_RETRIES}) ..." >&2
        sleep 5
        ;;
    esac
  done
else
  echo "WSO2-IS-KM already registered (id=${existing_km_id}), skipping."
fi

echo "Fetching live OpenAPI spec from bff ..."
curl -sf "${BFF_URL}/openapi/v1.json" -o /tmp/sevaone-openapi.json

echo "Checking whether SevaOne-API already exists ..."
existing_api_id="$(http_json "SevaOne-API existence check" -H "Authorization: Bearer ${token}" "${APIM_URL}/api/am/publisher/v4/apis?query=name:SevaOne-API" \
  | jq -r '.list[0].id // empty')"

if [ -n "${existing_api_id}" ]; then
  echo "SevaOne-API already imported (id=${existing_api_id}), skipping import/publish/deploy."
  api_id="${existing_api_id}"

  # Resync operations from bff's current live OpenAPI spec -- an existing API's operations
  # are a frozen snapshot from whenever it was first imported, so new/changed bff endpoints
  # (e.g. get-acknowledgement, test-token) never reach the gateway on their own. This updates
  # the API's working copy only; it does not deploy a revision by itself -- the authType/
  # gateway-secret/securityScheme steps below each re-fetch, re-apply their fix on top of the
  # resynced operations, and deploy, so the operations these steps' fixes rely on are always
  # in the deployed revision after a full run.
  echo "Resyncing SevaOne-API's operations from bff's current OpenAPI spec ..."
  http_json "SevaOne-API swagger resync" -X PUT -H "Authorization: Bearer ${token}" \
    "${APIM_URL}/api/am/publisher/v4/apis/${api_id}/swagger" \
    -F "apiDefinition=@/tmp/sevaone-openapi.json;type=application/json" >/dev/null
else
  cat > /tmp/api-additional-properties.json <<EOF
{
  "name": "SevaOne-API",
  "context": "/sevaone",
  "version": "1.0.0",
  "endpointConfig": {
    "endpoint_type": "http",
    "sandbox_endpoints": {"url": "${BFF_URL}"},
    "production_endpoints": {"url": "${BFF_URL}"}
  },
  "policies": ["Unlimited"],
  "gatewayType": "wso2/synapse",
  "transport": ["https"],
  "visibility": "PUBLIC"
}
EOF

  echo "Importing SevaOne-API from OpenAPI spec ..."
  import_json="$(import_api_with_self_heal "SevaOne-API import" "/sevaone/1.0.0" -H "Authorization: Bearer ${token}" \
    -F "file=@/tmp/sevaone-openapi.json;type=application/json" \
    -F "additionalProperties=@/tmp/api-additional-properties.json;type=application/json" \
    "${APIM_URL}/api/am/publisher/v4/apis/import-openapi")"
  api_id="$(echo "${import_json}" | jq -r '.id')"
  require_nonempty "SevaOne-API imported id" "${api_id}"
  echo "Imported API id=${api_id}"

  echo "Publishing ..."
  http_json "SevaOne-API publish" -X POST -H "Authorization: Bearer ${token}" \
    "${APIM_URL}/api/am/publisher/v4/apis/change-lifecycle?apiId=${api_id}&action=Publish" >/dev/null

  echo "Creating and deploying a revision ..."
  prune_old_revisions "${api_id}"
  rev_json="$(http_json "SevaOne-API revision creation" -X POST -H "Authorization: Bearer ${token}" -H 'Content-Type: application/json' \
    "${APIM_URL}/api/am/publisher/v4/apis/${api_id}/revisions" -d '{"description":"Initial revision"}')"
  revision_id="$(echo "${rev_json}" | jq -r '.id')"
  require_nonempty "SevaOne-API revision id" "${revision_id}"
  http_json "SevaOne-API revision deploy" -X POST -H "Authorization: Bearer ${token}" -H 'Content-Type: application/json' \
    "${APIM_URL}/api/am/publisher/v4/apis/${api_id}/deploy-revision?revisionId=${revision_id}" \
    -d '[{"name":"Default","vhost":"'"${GATEWAY_VHOST}"'","displayOnDevportal":true}]' >/dev/null

  echo "SevaOne-API imported, published and deployed (id=${api_id})."
fi

# --- authType=None on the operator/* operations still hosted directly on SevaOne-API -----
# Operator auth is bff's own cookie-based SevaOperator session, not a WSO2 OAuth2 token, so
# these must not require an OAuth2 token the way the rest of SevaOne-API's default-imported
# operations do (same reasoning already applied to the whole citizen API below, just scoped
# here to the operator paths -- the rest of SevaOne-API's hardening is a separate, already-
# tracked gap, see docs/DEPARTMENT_GATEWAY_HARDENING_GAP.md).
api_json="$(http_json "SevaOne-API fetch (operator authType)" -H "Authorization: Bearer ${token}" "${APIM_URL}/api/am/publisher/v4/apis/${api_id}")"
operator_authtype_dirty="$(echo "${api_json}" | jq -r '[.operations[]? | select((.target | startswith("/api/v1/operator")) and .authType != "None")] | length > 0')"
if [ "${operator_authtype_dirty}" = "true" ]; then
  echo "Setting authType=None on SevaOne-API's operator/* operations ..."
  api_updated="$(echo "${api_json}" | jq '.operations = [.operations[] | if (.target | startswith("/api/v1/operator")) then .authType = "None" else . end]')"
  http_json "SevaOne-API update (operator authType)" -X PUT -H "Authorization: Bearer ${token}" -H 'Content-Type: application/json' \
    "${APIM_URL}/api/am/publisher/v4/apis/${api_id}" -d "${api_updated}" >/dev/null
  prune_old_revisions "${api_id}"
  op_rev_json="$(http_json "SevaOne-API operator-authtype revision creation" -X POST -H "Authorization: Bearer ${token}" -H 'Content-Type: application/json' \
    "${APIM_URL}/api/am/publisher/v4/apis/${api_id}/revisions" -d '{"description":"operator/* authType=None"}')"
  op_revision_id="$(echo "${op_rev_json}" | jq -r '.id')"
  require_nonempty "SevaOne-API operator-authtype revision id" "${op_revision_id}"
  http_json "SevaOne-API operator-authtype revision deploy" -X POST -H "Authorization: Bearer ${token}" -H 'Content-Type: application/json' \
    "${APIM_URL}/api/am/publisher/v4/apis/${api_id}/deploy-revision?revisionId=${op_revision_id}" \
    -d '[{"name":"Default","vhost":"'"${GATEWAY_VHOST}"'","displayOnDevportal":true}]' >/dev/null
fi

# --- department gateway-secret stamp on the /api/v1/dept/* operations -------------------
# DeptGatewayAuthMiddleware (bff) rejects any /api/v1/dept/* request lacking this header --
# it's how bff knows a request genuinely came through APIM and not straight to bff's own
# port (nginx routes /api/v1/dept/* through this gateway context specifically so the stamp
# gets added -- see Docker-deploy/nginx/nginx.conf). Same addHeader mechanism already used
# for the citizen gateway secret below, scoped here to just the dept/* operations --
# operator/* and the rest of SevaOne-API are untouched.
api_json="$(http_json "SevaOne-API fetch (dept gateway secret check)" -H "Authorization: Bearer ${token}" "${APIM_URL}/api/am/publisher/v4/apis/${api_id}")"
dept_gw_dirty="$(echo "${api_json}" | jq -r --arg secret "${DEPT_GATEWAY_SECRET}" '
  [.operations[]? | select(.target | startswith("/api/v1/dept"))
    | select((.operationPolicies.request // []) | map(select(.policyName=="addHeader" and .parameters.headerName=="X-SevaOne-Dept-Gateway-Secret" and .parameters.headerValue==$secret)) | length == 0)
  ] | length > 0')"
if [ "${dept_gw_dirty}" = "true" ]; then
  echo "Looking up the addHeader operation policy (for the dept gateway-secret stamp) ..."
  dept_add_header_policy="$(http_json "Operation policies list (dept)" -H "Authorization: Bearer ${token}" "${APIM_URL}/api/am/publisher/v4/operation-policies?limit=100" \
    | jq -c '[.list[] | select(.name=="addHeader")][0] | {id, version}')"
  dept_add_header_id="$(echo "${dept_add_header_policy}" | jq -r '.id')"
  dept_add_header_version="$(echo "${dept_add_header_policy}" | jq -r '.version')"
  require_nonempty "addHeader policy id (dept)" "${dept_add_header_id}"

  echo "Stamping X-SevaOne-Dept-Gateway-Secret on SevaOne-API's dept/* operations ..."
  api_updated="$(echo "${api_json}" | jq \
    --arg policyId "${dept_add_header_id}" \
    --arg policyVersion "${dept_add_header_version}" \
    --arg secret "${DEPT_GATEWAY_SECRET}" \
    '.operations = [.operations[] | if (.target | startswith("/api/v1/dept")) then .operationPolicies.request = [{
       policyName: "addHeader",
       policyVersion: $policyVersion,
       policyId: $policyId,
       parameters: {headerName: "X-SevaOne-Dept-Gateway-Secret", headerValue: $secret}
     }] else . end]')"
  http_json "SevaOne-API update (dept gateway secret)" -X PUT -H "Authorization: Bearer ${token}" -H 'Content-Type: application/json' \
    "${APIM_URL}/api/am/publisher/v4/apis/${api_id}" -d "${api_updated}" >/dev/null
  prune_old_revisions "${api_id}"
  dept_gw_rev_json="$(http_json "SevaOne-API dept-gateway-secret revision creation" -X POST -H "Authorization: Bearer ${token}" -H 'Content-Type: application/json' \
    "${APIM_URL}/api/am/publisher/v4/apis/${api_id}/revisions" -d '{"description":"dept/* gateway-secret stamp"}')"
  dept_gw_revision_id="$(echo "${dept_gw_rev_json}" | jq -r '.id')"
  require_nonempty "SevaOne-API dept-gateway-secret revision id" "${dept_gw_revision_id}"
  http_json "SevaOne-API dept-gateway-secret revision deploy" -X POST -H "Authorization: Bearer ${token}" -H 'Content-Type: application/json' \
    "${APIM_URL}/api/am/publisher/v4/apis/${api_id}/deploy-revision?revisionId=${dept_gw_revision_id}" \
    -d '[{"name":"Default","vhost":"'"${GATEWAY_VHOST}"'","displayOnDevportal":true}]' >/dev/null
fi

# --- api_key-only securityScheme on SevaOne-API -------------------------------------------
# WSO2 APIM's default import leaves securityScheme as
# ["oauth_basic_auth_api_key_mandatory", "oauth2"] -- that combination requires an OAuth2
# token *in addition to* ApiKey, not ApiKey alone. backend/docs/DEPARTMENT_INTEGRATION_GUIDE.md
# documents ApiKey + DeptMachineAuth as the complete department credential set, no OAuth2 token
# involved anywhere -- so the gateway has to actually accept a bare ApiKey. This is the same gap
# item 1 of docs/DEPARTMENT_GATEWAY_HARDENING_GAP.md describes (narrowing authType off OAuth2),
# expressed at the API's securityScheme level rather than per-operation authType.
api_json="$(http_json "SevaOne-API fetch (securityScheme check)" -H "Authorization: Bearer ${token}" "${APIM_URL}/api/am/publisher/v4/apis/${api_id}")"
security_scheme_dirty="$(echo "${api_json}" | jq -r '(.securityScheme // []) != ["api_key"]')"
if [ "${security_scheme_dirty}" = "true" ]; then
  echo "Setting SevaOne-API's securityScheme to api_key-only ..."
  api_updated="$(echo "${api_json}" | jq '.securityScheme = ["api_key"]')"
  http_json "SevaOne-API update (securityScheme)" -X PUT -H "Authorization: Bearer ${token}" -H 'Content-Type: application/json' \
    "${APIM_URL}/api/am/publisher/v4/apis/${api_id}" -d "${api_updated}" >/dev/null
  prune_old_revisions "${api_id}"
  sec_rev_json="$(http_json "SevaOne-API securityScheme revision creation" -X POST -H "Authorization: Bearer ${token}" -H 'Content-Type: application/json' \
    "${APIM_URL}/api/am/publisher/v4/apis/${api_id}/revisions" -d '{"description":"api_key-only securityScheme"}')"
  sec_revision_id="$(echo "${sec_rev_json}" | jq -r '.id')"
  require_nonempty "SevaOne-API securityScheme revision id" "${sec_revision_id}"
  http_json "SevaOne-API securityScheme revision deploy" -X POST -H "Authorization: Bearer ${token}" -H 'Content-Type: application/json' \
    "${APIM_URL}/api/am/publisher/v4/apis/${api_id}/deploy-revision?revisionId=${sec_revision_id}" \
    -d '[{"name":"Default","vhost":"'"${GATEWAY_VHOST}"'","displayOnDevportal":true}]' >/dev/null
fi

echo "Checking whether SevaOne-Citizen-API already exists ..."
existing_citizen_api_id="$(http_json "SevaOne-Citizen-API existence check" -H "Authorization: Bearer ${token}" "${APIM_URL}/api/am/publisher/v4/apis?query=name:SevaOne-Citizen-API" \
  | jq -r '.list[0].id // empty')"

echo "Filtering OpenAPI spec down to citizen-facing paths ..."
# Mirrors CitizenGatewayAuthMiddleware.CitizenPathPrefixes exactly -- keep both in sync.
python3 <<'PYEOF'
import json
d = json.load(open('/tmp/sevaone-openapi.json'))
prefixes = ('/api/v1/auth', '/api/v1/me', '/api/v1/departments', '/api/v1/schemes',
            '/api/v1/search', '/api/v1/applications', '/api/v1/track', '/api/v1/apply')
d['paths'] = {p: v for p, v in d['paths'].items() if p.startswith(prefixes)}
d['info']['title'] = 'SevaOne-Citizen-API'
json.dump(d, open('/tmp/sevaone-citizen-openapi.json', 'w'))
print(f"kept {len(d['paths'])} citizen paths")
PYEOF

if [ -n "${existing_citizen_api_id}" ]; then
  # Same gap SevaOne-API's own resync branch above already fixed once: an existing API's
  # operations are a frozen snapshot from whenever it was first imported, so a new bff
  # endpoint under a citizen prefix (e.g. this session's /me/audit) would otherwise 404 at
  # the gateway forever, never even reaching bff, however many times this script re-runs --
  # confirmed live (curl to /api/v1/me/audit: 401 straight to bff, 404 through this gateway).
  # Unlike SevaOne-API, this only updates the working copy -- authType/addHeader/deploy below
  # (previously fresh-import-only) now always re-run too, so a resync alone isn't silently
  # inert (the new operation would resync into the API but stay unreachable: still missing
  # the authType=None + gateway-secret header policy every operation here needs).
  echo "SevaOne-Citizen-API already imported (id=${existing_citizen_api_id}) -- resyncing its operations from bff's current OpenAPI spec ..."
  citizen_api_id="${existing_citizen_api_id}"
  citizen_api_freshly_imported=false
  http_json "SevaOne-Citizen-API swagger resync" -X PUT -H "Authorization: Bearer ${token}" \
    "${APIM_URL}/api/am/publisher/v4/apis/${citizen_api_id}/swagger" \
    -F "apiDefinition=@/tmp/sevaone-citizen-openapi.json;type=application/json" >/dev/null
else
  cat > /tmp/citizen-api-additional-properties.json <<EOF
{
  "name": "SevaOne-Citizen-API",
  "context": "/sevaone-citizen",
  "version": "1.0.0",
  "endpointConfig": {
    "endpoint_type": "http",
    "sandbox_endpoints": {"url": "${BFF_URL}"},
    "production_endpoints": {"url": "${BFF_URL}"}
  },
  "policies": ["Unlimited"],
  "gatewayType": "wso2/synapse",
  "transport": ["https"],
  "visibility": "PUBLIC"
}
EOF

  echo "Importing SevaOne-Citizen-API from filtered OpenAPI spec ..."
  citizen_import_json="$(import_api_with_self_heal "SevaOne-Citizen-API import" "/sevaone-citizen/1.0.0" -H "Authorization: Bearer ${token}" \
    -F "file=@/tmp/sevaone-citizen-openapi.json;type=application/json" \
    -F "additionalProperties=@/tmp/citizen-api-additional-properties.json;type=application/json" \
    "${APIM_URL}/api/am/publisher/v4/apis/import-openapi")"
  citizen_api_id="$(echo "${citizen_import_json}" | jq -r '.id')"
  require_nonempty "SevaOne-Citizen-API imported id" "${citizen_api_id}"
  citizen_api_freshly_imported=true
  echo "Imported API id=${citizen_api_id}"
fi

# From here down, everything runs unconditionally (resync or fresh import alike): a new
# operation resynced into an existing API still needs authType=None + the gateway-secret
# header policy applied, and a new revision deployed, or it stays unreachable through the
# gateway despite resyncing successfully in the step above.
echo "Looking up the addHeader operation policy ..."
add_header_policy="$(http_json "Operation policies list" -H "Authorization: Bearer ${token}" "${APIM_URL}/api/am/publisher/v4/operation-policies?limit=100" \
  | jq -c '[.list[] | select(.name=="addHeader")][0] | {id, version}')"
add_header_id="$(echo "${add_header_policy}" | jq -r '.id')"
add_header_version="$(echo "${add_header_policy}" | jq -r '.version')"
require_nonempty "addHeader policy id" "${add_header_id}"

echo "Setting authType=None and attaching the gateway-secret header policy on every operation ..."
citizen_api_json="$(http_json "SevaOne-Citizen-API fetch" -H "Authorization: Bearer ${token}" "${APIM_URL}/api/am/publisher/v4/apis/${citizen_api_id}")"
citizen_api_updated="$(echo "${citizen_api_json}" | jq \
  --arg policyId "${add_header_id}" \
  --arg policyVersion "${add_header_version}" \
  --arg secret "${CITIZEN_GATEWAY_SECRET}" \
  '.apiThrottlingPolicy = "20KPerMin" |
   .operations = [.operations[] | .authType = "None" | .operationPolicies.request = [{
     policyName: "addHeader",
     policyVersion: $policyVersion,
     policyId: $policyId,
     parameters: {headerName: "X-SevaOne-Citizen-Gateway-Secret", headerValue: $secret}
   }]]')"
http_json "SevaOne-Citizen-API update" -X PUT -H "Authorization: Bearer ${token}" -H 'Content-Type: application/json' \
  "${APIM_URL}/api/am/publisher/v4/apis/${citizen_api_id}" -d "${citizen_api_updated}" >/dev/null

# Publish is a one-time lifecycle transition (CREATED -> PUBLISHED) -- an already-published
# API from a prior run doesn't need it again, only a new revision deployed (below).
if [ "${citizen_api_freshly_imported}" = true ]; then
  echo "Publishing ..."
  http_json "SevaOne-Citizen-API publish" -X POST -H "Authorization: Bearer ${token}" \
    "${APIM_URL}/api/am/publisher/v4/apis/change-lifecycle?apiId=${citizen_api_id}&action=Publish" >/dev/null
fi

echo "Creating and deploying a revision ..."
prune_old_revisions "${citizen_api_id}"
citizen_rev_json="$(http_json "SevaOne-Citizen-API revision creation" -X POST -H "Authorization: Bearer ${token}" -H 'Content-Type: application/json' \
  "${APIM_URL}/api/am/publisher/v4/apis/${citizen_api_id}/revisions" -d '{"description":"Resync from bff OpenAPI spec"}')"
citizen_revision_id="$(echo "${citizen_rev_json}" | jq -r '.id')"
require_nonempty "SevaOne-Citizen-API revision id" "${citizen_revision_id}"
http_json "SevaOne-Citizen-API revision deploy" -X POST -H "Authorization: Bearer ${token}" -H 'Content-Type: application/json' \
  "${APIM_URL}/api/am/publisher/v4/apis/${citizen_api_id}/deploy-revision?revisionId=${citizen_revision_id}" \
  -d '[{"name":"Default","vhost":"'"${GATEWAY_VHOST}"'","displayOnDevportal":true}]' >/dev/null

echo "SevaOne-Citizen-API ready (id=${citizen_api_id})."

echo "Restart the wso2apim container once so the gateway picks up any new deployments"
echo "above (APIM's deployment-sync events don't reliably reach the running gateway --"
echo "see backend/docs/WSO2_IS_APIM_SETUP.md, Gotcha 2)."
