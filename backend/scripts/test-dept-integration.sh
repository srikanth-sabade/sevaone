#!/usr/bin/env bash
# Manual curl walkthrough of backend/docs/DEPARTMENT_INTEGRATION_GUIDE.md's four /dept/* calls,
# against the real WSO2 APIM gateway (ApiKey + WSO2 IS Bearer token, exactly like a real
# department would call SevaOne) -- not a shortcut straight to bff.
#
# Usage:
#   GW_HOST=100.91.197.105 DEPT=revenue SERVICE_ID=REVENUE-2311 \
#   WSO2_IS_URL=https://100.91.197.105:9443 \
#   DEPT_CLIENT_ID=... DEPT_CLIENT_SECRET=... API_KEY=... \
#   ./test-dept-integration.sh
#
# DEPT_CLIENT_ID/SECRET: from WSO2 IS's 'SevaOne_Dept_<DEPT>_Client' application
#   (GET /api/server/v1/applications/<id>/inbound-protocols/oidc as admin/admin).
# API_KEY: generate via WSO2 APIM Devportal -- create an Application (throttlingPolicy
#   "Unlimited"), subscribe it to SevaOne-API, then POST .../api-keys/PRODUCTION/generate
#   with {"keyName":"...", "validityPeriod": 3600}. See backend/docs/WSO2_IS_APIM_SETUP.md
#   Section "Department onboarding" for the full curl sequence.
set -euo pipefail

GW_HOST="${GW_HOST:?Set GW_HOST (e.g. 100.91.197.105 or localhost)}"
DEPT="${DEPT:?Set DEPT (e.g. revenue)}"
SERVICE_ID="${SERVICE_ID:?Set SERVICE_ID (a real service_id for that department with a deeplink_citizen_url configured)}"
WSO2_IS_URL="${WSO2_IS_URL:-https://${GW_HOST}:9443}"
DEPT_CLIENT_ID="${DEPT_CLIENT_ID:?Set DEPT_CLIENT_ID}"
DEPT_CLIENT_SECRET="${DEPT_CLIENT_SECRET:?Set DEPT_CLIENT_SECRET}"
API_KEY="${API_KEY:?Set API_KEY}"
GW="https://${GW_HOST}:8244/sevaone/1.0.0"

echo "== 0. Get Bearer token (WSO2 IS, client_credentials, scope=dept:${DEPT}) =="
TOKEN_RESP="$(curl -sk -u "${DEPT_CLIENT_ID}:${DEPT_CLIENT_SECRET}" \
  -X POST "${WSO2_IS_URL}/oauth2/token" \
  -d "grant_type=client_credentials&scope=dept:${DEPT}")"
echo "${TOKEN_RESP}" | python3 -m json.tool
BEARER="$(echo "${TOKEN_RESP}" | python3 -c "import json,sys; print(json.load(sys.stdin)['access_token'])")"
echo

echo "== 1. test-token (mint a token for a fixed test citizen) =="
TT="$(curl -sk -X POST "${GW}/api/v1/dept/test-token" \
  -H "ApiKey: ${API_KEY}" -H "Authorization: Bearer ${BEARER}" -H "Content-Type: application/json" \
  -d "{\"departmentId\":\"${DEPT}\",\"serviceId\":\"${SERVICE_ID}\"}")"
echo "${TT}" | python3 -m json.tool
APPLY_URL="$(echo "${TT}" | python3 -c "import json,sys; print(json.load(sys.stdin)['applyUrl'])")"
TOKEN="$(python3 -c "from urllib.parse import urlparse, parse_qs; print(parse_qs(urlparse('${APPLY_URL}').query)['Token'][0])")"
echo

# NOTE: Payment Status shares the same token record as User Details' single-use redemption --
# calling User Details first will revoke the token and Payment Status will then fail with
# SEVA-DEPT-4001, even though the guide documents Payment Status as independently callable with
# its own (midnight) validity window. Mint a second token below if you need to test Payment
# Status after having already called User Details once. See PR/issue tracking this gap in
# DeptTokenGate.ResolveAsync (shared IsActive/RevokedAt check for both call types).

echo "== 2. user-details (consumes the token -- single use) =="
curl -sk -X POST "${GW}/api/v1/dept/user-details" \
  -H "ApiKey: ${API_KEY}" -H "Authorization: Bearer ${BEARER}" -H "Content-Type: application/json" \
  -d "{\"departmentId\":\"${DEPT}\",\"token\":\"${TOKEN}\"}" | python3 -m json.tool
echo

echo "== 3. get-acknowledgement =="
ACK_ID="TEST-ACK-$(date +%s)"
curl -sk -X POST "${GW}/api/v1/dept/get-acknowledgement" \
  -H "ApiKey: ${API_KEY}" -H "Authorization: Bearer ${BEARER}" -H "Content-Type: application/json" \
  -d "{\"departmentId\":\"${DEPT}\",\"sevaOneUserId\":1,\"serviceId\":\"${SERVICE_ID}\",\"status\":\"Submitted\",\"acknowledgementId\":\"${ACK_ID}\"}" \
  | python3 -m json.tool
echo

echo "== 4. status-update (webhook, keyed on acknowledgementId) =="
curl -sk -X POST "${GW}/api/v1/dept/status-update" \
  -H "ApiKey: ${API_KEY}" -H "Authorization: Bearer ${BEARER}" -H "Content-Type: application/json" \
  -d "{\"departmentId\":\"${DEPT}\",\"acknowledgementId\":\"${ACK_ID}\",\"status\":\"UnderReview\",\"note\":\"Manual curl test - document verification completed\"}" \
  | python3 -m json.tool
echo

echo "== 5. payment-status (mint a FRESH token -- must run before that token's User Details call, see NOTE above) =="
TT2="$(curl -sk -X POST "${GW}/api/v1/dept/test-token" \
  -H "ApiKey: ${API_KEY}" -H "Authorization: Bearer ${BEARER}" -H "Content-Type: application/json" \
  -d "{\"departmentId\":\"${DEPT}\",\"serviceId\":\"${SERVICE_ID}\"}")"
APPLY_URL2="$(echo "${TT2}" | python3 -c "import json,sys; print(json.load(sys.stdin)['applyUrl'])")"
TOKEN2="$(python3 -c "from urllib.parse import urlparse, parse_qs; print(parse_qs(urlparse('${APPLY_URL2}').query)['Token'][0])")"
curl -sk -X POST "${GW}/api/v1/dept/payment-status" \
  -H "ApiKey: ${API_KEY}" -H "Authorization: Bearer ${BEARER}" -H "Content-Type: application/json" \
  -d "{\"departmentId\":\"${DEPT}\",\"token\":\"${TOKEN2}\",\"paymentStatus\":\"Success\",\"amount\":250.00,\"transactionId\":\"${ACK_ID}-fee\"}" \
  | python3 -m json.tool
