#!/bin/sh
# Runs once after wso2is is up. Registers the SevaOne citizen-issuer JWT-bearer
# exchange (RFC 7523) that bff uses to turn a DigiLocker/mobile-OTP-verified
# citizen into a real WSO2 access/refresh token pair -- see
# backend/SevaOne.Infrastructure/ExternalAuth/Wso2CitizenIssuerOptions.cs and
# backend/docs/WSO2_IS_APIM_SETUP.md ("Citizen authentication") for the full
# picture. Two WSO2 IS objects are involved:
#   1. A Trusted Token Issuer (identity provider) holding the PUBLIC half of
#      the signing keypair -- this is what lets WSO2 trust JWTs bff signs.
#      name/alias are hardcoded on both sides (see Wso2CitizenIssuerOptions
#      defaults) so no id/secret needs to flow back for this one.
#   2. An OAuth application with the jwt-bearer grant enabled -- WSO2
#      auto-generates its clientId/clientSecret, which DO need to flow back
#      into Docker-deploy/.env for bff to use.
#
# Idempotent: safe to re-run against an already-provisioned WSO2 IS (skips
# anything that already exists) and is the only way to re-provision after a
# full reset (wso2is's Postgres databases wiped) since that also wipes the
# app/idp along with everything else.
#
# Needs curl + jq on PATH (the wso2is-provision service installs both via apk
# before running this -- see docker-compose.apps.yml).
set -eu

IS_URL="${IS_URL:-https://wso2is:9443}"
ADMIN_USER="${ADMIN_USER:-admin}"
ADMIN_PASS="${ADMIN_PASS:-admin}"
CERT_PATH="${CERT_PATH:-/citizen-issuer.crt}"

IDP_NAME="SevaOne Citizen Issuer"
IDP_ALIAS="sevaone-citizen-identity"
APP_NAME="SevaOne_Citizen_Issuer_Client"
CALLBACK_URL="https://localhost/citizen-issuer-callback"

echo "Waiting for WSO2 IS at ${IS_URL} ..."
i=0
until curl -skf "${IS_URL}/carbon/admin/login.jsp" >/dev/null 2>&1; do
  i=$((i + 1))
  if [ "${i}" -ge 90 ]; then echo "WSO2 IS did not come up in time" >&2; exit 1; fi
  echo "    still waiting for WSO2 IS ... (attempt ${i}/90, $((i * 5))s elapsed)"
  sleep 5
done
echo "WSO2 IS reachable (attempt ${i})."

echo "Checking whether the '${IDP_NAME}' trusted token issuer already exists ..."
existing_idp_id="$(curl -sk -u "${ADMIN_USER}:${ADMIN_PASS}" "${IS_URL}/api/server/v1/identity-providers?limit=100" \
  | jq -r --arg name "${IDP_NAME}" '[.identityProviders[]? | select(.name==$name)][0].id // empty')"

if [ -n "${existing_idp_id}" ]; then
  echo "Trusted token issuer already registered (id=${existing_idp_id}), skipping."
else
  echo "Registering '${IDP_NAME}' as a trusted token issuer ..."
  cert_b64="$(base64 -w0 "${CERT_PATH}")"
  # No "isEnabled" field here -- WSO2 IS 7.3.0 defaults new IdPs to enabled, and rejects
  # a create request that specifies it explicitly with the same generic 400 "not in the
  # expected format" (confirmed empirically: identical body minus this one field succeeds).
  curl -sk -f -X POST -u "${ADMIN_USER}:${ADMIN_PASS}" -H 'Content-Type: application/json' \
    "${IS_URL}/api/server/v1/identity-providers" -d '{
      "name": "'"${IDP_NAME}"'",
      "description": "Trusted issuer for SevaOne citizen JWT-bearer token exchange",
      "alias": "'"${IDP_ALIAS}"'",
      "certificate": {"certificates": ["'"${cert_b64}"'"]}
    }' >/dev/null
  echo "Trusted token issuer registered."
fi

echo "Checking whether the '${APP_NAME}' application already exists ..."
existing_app_id="$(curl -sk -u "${ADMIN_USER}:${ADMIN_PASS}" "${IS_URL}/api/server/v1/applications?filter=name+eq+${APP_NAME}" \
  | jq -r '.applications[0].id // empty')"

if [ -z "${existing_app_id}" ]; then
  echo "Creating application '${APP_NAME}' ..."
  location="$(curl -sk -D - -o /dev/null -X POST -u "${ADMIN_USER}:${ADMIN_PASS}" -H 'Content-Type: application/json' \
    "${IS_URL}/api/server/v1/applications" -d '{
      "name": "'"${APP_NAME}"'",
      "description": "SevaOne backend JWT-bearer token exchange client",
      "advancedConfigurations": {"skipLoginConsent": true, "skipLogoutConsent": true}
    }' | tr -d '\r' | awk '/^[Ll]ocation:/ {print $2}')"
  app_id="${location##*/}"
  echo "Application created (id=${app_id})."
else
  app_id="${existing_app_id}"
  echo "Application already exists (id=${app_id})."
fi

echo "Checking whether the OIDC inbound config is already set up ..."
oidc_status="$(curl -sk -o /tmp/oidc-config.json -w '%{http_code}' -u "${ADMIN_USER}:${ADMIN_PASS}" \
  "${IS_URL}/api/server/v1/applications/${app_id}/inbound-protocols/oidc")"

if [ "${oidc_status}" = "200" ]; then
  echo "OIDC inbound config already present, leaving it as-is (re-PUTting would be rejected"
  echo "with 'Invalid ClientID provided for update' -- WSO2 requires clientId in the body"
  echo "once a config already exists, and we don't want to touch a working one)."
else
  echo "Configuring OIDC inbound protocol (grantTypes, jwt-bearer, allowedOrigins) ..."
  # Field is "allowedOrigins", NOT "accessControlAllowOrigins" -- the latter looks
  # plausible (it's the literal CORS header name) but is rejected with HTTP 400
  # "Provided request body content is not in the expected format." Omitting the
  # field entirely instead throws a real WSO2 IS 7.3.0 NPE server-side (500,
  # CORSConfigurationUtils.hasDuplicates on a null collection) -- confirmed against
  # WSO2's own "Console" app config, which uses this exact field name. Also: no
  # clientId in this body -- that's only required/accepted on a REPEAT update.
  curl -sk -f -X PUT -u "${ADMIN_USER}:${ADMIN_PASS}" -H 'Content-Type: application/json' \
    "${IS_URL}/api/server/v1/applications/${app_id}/inbound-protocols/oidc" -d '{
      "grantTypes": ["client_credentials", "urn:ietf:params:oauth:grant-type:jwt-bearer", "refresh_token"],
      "callbackURLs": ["'"${CALLBACK_URL}"'"],
      "publicClient": false,
      "pkce": {"mandatory": false, "supportPlainTransformAlgorithm": false},
      "allowedOrigins": []
    }' >/dev/null
  curl -sk -u "${ADMIN_USER}:${ADMIN_PASS}" -o /tmp/oidc-config.json \
    "${IS_URL}/api/server/v1/applications/${app_id}/inbound-protocols/oidc"
  echo "OIDC inbound config created."
fi

client_id="$(jq -r '.clientId' /tmp/oidc-config.json)"
client_secret="$(jq -r '.clientSecret' /tmp/oidc-config.json)"

echo "RESULT_CLIENT_ID=${client_id}"
echo "RESULT_CLIENT_SECRET=${client_secret}"

# --- Operator identity: real WSO2 IS accounts, password-grant login, "SevaOne_Operator" ---
# SCIM2 group as the RBAC signal -- see backend/docs/WSO2_IS_ROADMAP.md section 2.3 and
# backend/SevaOne.Infrastructure/ExternalAuth/Wso2OperatorIssuerOptions.cs. Two objects, both
# distinct from the citizen ones above (operators authenticate directly with WSO2, citizens
# never do):
#   1. An OAuth application with the password grant enabled -- the only grant type that fits a
#      plain username/password kiosk login form (no browser redirect). clientId/clientSecret
#      flow back into Docker-deploy/.env for bff to use.
#   2. A SCIM2 group "SevaOne_Operator" -- membership in it IS the RBAC check. Confirmed
#      empirically that WSO2 IS 7.3.0 does NOT surface group/role membership in the password
#      grant's id_token or in /oauth2/introspect by default (tried both: requesting the
#      http://wso2.org/claims/role claim on the app made no difference to either). The reliable
#      path is a plain SCIM2 GET /scim2/Users/{id} with admin credentials after a successful
#      password-grant login, checking the returned `groups` array -- same admin-credentialed
#      GET pattern Wso2CitizenIdentityClient already uses, just checked instead of
#      find-or-created.
OPERATOR_APP_NAME="SevaOne_Operator_Auth_Client"
OPERATOR_GROUP_NAME="SevaOne_Operator"

echo "Checking whether the '${OPERATOR_APP_NAME}' application already exists ..."
existing_operator_app_id="$(curl -sk -u "${ADMIN_USER}:${ADMIN_PASS}" "${IS_URL}/api/server/v1/applications?filter=name+eq+${OPERATOR_APP_NAME}" \
  | jq -r '.applications[0].id // empty')"

if [ -z "${existing_operator_app_id}" ]; then
  echo "Creating application '${OPERATOR_APP_NAME}' ..."
  operator_location="$(curl -sk -D - -o /dev/null -X POST -u "${ADMIN_USER}:${ADMIN_PASS}" -H 'Content-Type: application/json' \
    "${IS_URL}/api/server/v1/applications" -d '{
      "name": "'"${OPERATOR_APP_NAME}"'",
      "description": "SevaOne backend password-grant client for operator login",
      "advancedConfigurations": {"skipLoginConsent": true, "skipLogoutConsent": true}
    }' | tr -d '\r' | awk '/^[Ll]ocation:/ {print $2}')"
  operator_app_id="${operator_location##*/}"
  echo "Application created (id=${operator_app_id})."
else
  operator_app_id="${existing_operator_app_id}"
  echo "Application already exists (id=${operator_app_id})."
fi

echo "Checking whether the operator app's OIDC inbound config is already set up ..."
operator_oidc_status="$(curl -sk -o /tmp/operator-oidc-config.json -w '%{http_code}' -u "${ADMIN_USER}:${ADMIN_PASS}" \
  "${IS_URL}/api/server/v1/applications/${operator_app_id}/inbound-protocols/oidc")"

if [ "${operator_oidc_status}" = "200" ]; then
  echo "Operator OIDC inbound config already present, leaving it as-is (same reason as the"
  echo "citizen app above -- re-PUTting an existing config needs clientId in the body)."
else
  echo "Configuring operator app's OIDC inbound protocol (password grant) ..."
  curl -sk -f -X PUT -u "${ADMIN_USER}:${ADMIN_PASS}" -H 'Content-Type: application/json' \
    "${IS_URL}/api/server/v1/applications/${operator_app_id}/inbound-protocols/oidc" -d '{
      "grantTypes": ["password", "refresh_token"],
      "publicClient": false,
      "pkce": {"mandatory": false, "supportPlainTransformAlgorithm": false},
      "allowedOrigins": []
    }' >/dev/null
  curl -sk -u "${ADMIN_USER}:${ADMIN_PASS}" -o /tmp/operator-oidc-config.json \
    "${IS_URL}/api/server/v1/applications/${operator_app_id}/inbound-protocols/oidc"
  echo "Operator OIDC inbound config created."
fi

operator_client_id="$(jq -r '.clientId' /tmp/operator-oidc-config.json)"
operator_client_secret="$(jq -r '.clientSecret' /tmp/operator-oidc-config.json)"

echo "Checking whether the '${OPERATOR_GROUP_NAME}' SCIM2 group already exists ..."
existing_operator_group_id="$(curl -sk -u "${ADMIN_USER}:${ADMIN_PASS}" \
  "${IS_URL}/scim2/Groups?filter=displayName+eq+${OPERATOR_GROUP_NAME}" \
  | jq -r '.Resources[0].id // empty')"

if [ -z "${existing_operator_group_id}" ]; then
  echo "Creating SCIM2 group '${OPERATOR_GROUP_NAME}' ..."
  curl -sk -f -X POST -u "${ADMIN_USER}:${ADMIN_PASS}" -H 'Content-Type: application/scim+json' \
    "${IS_URL}/scim2/Groups" -d '{
      "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
      "displayName": "'"${OPERATOR_GROUP_NAME}"'"
    }' >/dev/null
  echo "SCIM2 group created."
else
  echo "SCIM2 group already exists (id=${existing_operator_group_id}), skipping."
fi

echo "RESULT_OPERATOR_CLIENT_ID=${operator_client_id}"
echo "RESULT_OPERATOR_CLIENT_SECRET=${operator_client_secret}"

# --- Gov-admin identity: real WSO2 IS accounts, password-grant login, "SevaOne_GovAdmin" ------
# Same existing userstore, same shape as the operator block above -- NOT a secondary userstore.
# Deliberate: a real secondary JDBC userstore was considered and rejected for this feature (see
# SESSION_HANDOFF.md's "Government Admin Panel" section) because WSO2 IS 7.3.0 doesn't surface
# SCIM2 group membership in the token regardless of userstore, so a claims-only design would
# depend on unverified WSO2 behavior either way. Fine-grained tier/district/taluk RBAC lives in
# portal.gov_admin_master (local Postgres), linked by wso2_subject -- this app + group are only
# the coarse WSO2-side gate, exactly like SevaOne_Operator_Auth_Client/SevaOne_Operator above.
GOVADMIN_APP_NAME="SevaOne_GovAdmin_Auth_Client"
GOVADMIN_GROUP_NAME="SevaOne_GovAdmin"

echo "Checking whether the '${GOVADMIN_APP_NAME}' application already exists ..."
existing_govadmin_app_id="$(curl -sk -u "${ADMIN_USER}:${ADMIN_PASS}" "${IS_URL}/api/server/v1/applications?filter=name+eq+${GOVADMIN_APP_NAME}" \
  | jq -r '.applications[0].id // empty')"

if [ -z "${existing_govadmin_app_id}" ]; then
  echo "Creating application '${GOVADMIN_APP_NAME}' ..."
  govadmin_location="$(curl -sk -D - -o /dev/null -X POST -u "${ADMIN_USER}:${ADMIN_PASS}" -H 'Content-Type: application/json' \
    "${IS_URL}/api/server/v1/applications" -d '{
      "name": "'"${GOVADMIN_APP_NAME}"'",
      "description": "SevaOne backend password-grant client for the government admin panel",
      "advancedConfigurations": {"skipLoginConsent": true, "skipLogoutConsent": true}
    }' | tr -d '\r' | awk '/^[Ll]ocation:/ {print $2}')"
  govadmin_app_id="${govadmin_location##*/}"
  echo "Application created (id=${govadmin_app_id})."
else
  govadmin_app_id="${existing_govadmin_app_id}"
  echo "Application already exists (id=${govadmin_app_id})."
fi

echo "Checking whether the gov-admin app's OIDC inbound config is already set up ..."
govadmin_oidc_status="$(curl -sk -o /tmp/govadmin-oidc-config.json -w '%{http_code}' -u "${ADMIN_USER}:${ADMIN_PASS}" \
  "${IS_URL}/api/server/v1/applications/${govadmin_app_id}/inbound-protocols/oidc")"

if [ "${govadmin_oidc_status}" = "200" ]; then
  echo "Gov-admin OIDC inbound config already present, leaving it as-is (same reason as the"
  echo "citizen/operator apps above -- re-PUTting an existing config needs clientId in the body)."
else
  echo "Configuring gov-admin app's OIDC inbound protocol (password grant) ..."
  curl -sk -f -X PUT -u "${ADMIN_USER}:${ADMIN_PASS}" -H 'Content-Type: application/json' \
    "${IS_URL}/api/server/v1/applications/${govadmin_app_id}/inbound-protocols/oidc" -d '{
      "grantTypes": ["password", "refresh_token"],
      "publicClient": false,
      "pkce": {"mandatory": false, "supportPlainTransformAlgorithm": false},
      "allowedOrigins": []
    }' >/dev/null
  curl -sk -u "${ADMIN_USER}:${ADMIN_PASS}" -o /tmp/govadmin-oidc-config.json \
    "${IS_URL}/api/server/v1/applications/${govadmin_app_id}/inbound-protocols/oidc"
  echo "Gov-admin OIDC inbound config created."
fi

govadmin_client_id="$(jq -r '.clientId' /tmp/govadmin-oidc-config.json)"
govadmin_client_secret="$(jq -r '.clientSecret' /tmp/govadmin-oidc-config.json)"

echo "Checking whether the '${GOVADMIN_GROUP_NAME}' SCIM2 group already exists ..."
existing_govadmin_group_id="$(curl -sk -u "${ADMIN_USER}:${ADMIN_PASS}" \
  "${IS_URL}/scim2/Groups?filter=displayName+eq+${GOVADMIN_GROUP_NAME}" \
  | jq -r '.Resources[0].id // empty')"

if [ -z "${existing_govadmin_group_id}" ]; then
  echo "Creating SCIM2 group '${GOVADMIN_GROUP_NAME}' ..."
  curl -sk -f -X POST -u "${ADMIN_USER}:${ADMIN_PASS}" -H 'Content-Type: application/scim+json' \
    "${IS_URL}/scim2/Groups" -d '{
      "schemas": ["urn:ietf:params:scim:schemas:core:2.0:Group"],
      "displayName": "'"${GOVADMIN_GROUP_NAME}"'"
    }' >/dev/null
  echo "SCIM2 group created."
else
  echo "SCIM2 group already exists (id=${existing_govadmin_group_id}), skipping."
fi

echo "RESULT_GOVADMIN_CLIENT_ID=${govadmin_client_id}"
echo "RESULT_GOVADMIN_CLIENT_SECRET=${govadmin_client_secret}"

# --- Department auth: one WSO2 IS client_credentials app + one dept:<code> scope per --------
# department. Replaces the static Jws:DeptClients Basic-auth list (backend/appsettings.json) --
# a department gets a real WSO2 IS credential instead, and disabling its application in WSO2 IS
# is now a live kill switch: DeptController.TryResolveDepartmentAsync introspects the caller's
# Bearer token (via IWso2TokenIntrospectionCache, same Redis-cached pattern the citizen auth
# middleware already uses) and reads its dept:<code> scope to identify the department -- no
# static list, no redeploy to revoke. A department calls this app's own client_credentials grant
# to get its Bearer token, same mechanism as the SevaOne_Operator_Auth_Client app above, just
# client_credentials instead of password.
#
# WSO2 IS drops unregistered custom scope strings from a token response silently (no error,
# confirmed live) -- the scope registration step below is not optional, it's why dept:<code>
# actually ends up on the issued token at all.
DEPT_CODES="revenue health rdpr kaic food transport education social-welfare wcd labour agriculture"

for dept in ${DEPT_CODES}; do
  scope_name="dept:${dept}"
  echo "Checking whether the '${scope_name}' OAuth2 scope already exists ..."
  existing_scope="$(curl -sk -u "${ADMIN_USER}:${ADMIN_PASS}" "${IS_URL}/api/identity/oauth2/v1.0/scopes" \
    | jq -r --arg n "${scope_name}" '[.[] | select(.name==$n)] | length > 0')"

  if [ "${existing_scope}" != "true" ]; then
    echo "Registering '${scope_name}' scope ..."
    curl -sk -f -X POST -u "${ADMIN_USER}:${ADMIN_PASS}" -H 'Content-Type: application/json' \
      "${IS_URL}/api/identity/oauth2/v1.0/scopes" -d '{
        "name": "'"${scope_name}"'",
        "displayName": "'"${dept}"' department access",
        "description": "Identifies the '"${dept}"' department for dept-auth token binding"
      }' >/dev/null
  else
    echo "'${scope_name}' scope already exists, skipping."
  fi

  dept_app_name="SevaOne_Dept_$(echo "${dept}" | tr '[:lower:]-' '[:upper:]_')_Client"
  echo "Checking whether application '${dept_app_name}' already exists ..."
  existing_dept_app_id="$(curl -sk -u "${ADMIN_USER}:${ADMIN_PASS}" "${IS_URL}/api/server/v1/applications?filter=name+eq+${dept_app_name}" \
    | jq -r '.applications[0].id // empty')"

  if [ -z "${existing_dept_app_id}" ]; then
    echo "Creating application '${dept_app_name}' ..."
    dept_location="$(curl -sk -D - -o /dev/null -X POST -u "${ADMIN_USER}:${ADMIN_PASS}" -H 'Content-Type: application/json' \
      "${IS_URL}/api/server/v1/applications" -d '{
        "name": "'"${dept_app_name}"'",
        "description": "client_credentials application for the '"${dept}"' department dept-facing API integration",
        "advancedConfigurations": {"skipLoginConsent": true, "skipLogoutConsent": true}
      }' | tr -d '\r' | awk '/^[Ll]ocation:/ {print $2}')"
    dept_app_id="${dept_location##*/}"
    echo "Application created (id=${dept_app_id})."
  else
    dept_app_id="${existing_dept_app_id}"
    echo "Application already exists (id=${dept_app_id})."
  fi

  echo "Checking whether '${dept_app_name}' OIDC inbound config is already set up ..."
  dept_oidc_status="$(curl -sk -o /tmp/dept-oidc-config.json -w '%{http_code}' -u "${ADMIN_USER}:${ADMIN_PASS}" \
    "${IS_URL}/api/server/v1/applications/${dept_app_id}/inbound-protocols/oidc")"

  if [ "${dept_oidc_status}" = "200" ]; then
    echo "'${dept_app_name}' OIDC inbound config already present, leaving it as-is."
  else
    echo "Configuring '${dept_app_name}' OIDC inbound protocol (client_credentials grant) ..."
    curl -sk -f -X PUT -u "${ADMIN_USER}:${ADMIN_PASS}" -H 'Content-Type: application/json' \
      "${IS_URL}/api/server/v1/applications/${dept_app_id}/inbound-protocols/oidc" -d '{
        "grantTypes": ["client_credentials"],
        "publicClient": false,
        "pkce": {"mandatory": false, "supportPlainTransformAlgorithm": false},
        "allowedOrigins": []
      }' >/dev/null
    curl -sk -u "${ADMIN_USER}:${ADMIN_PASS}" -o /tmp/dept-oidc-config.json \
      "${IS_URL}/api/server/v1/applications/${dept_app_id}/inbound-protocols/oidc"
  fi

  dept_client_id="$(jq -r '.clientId' /tmp/dept-oidc-config.json)"
  dept_client_secret="$(jq -r '.clientSecret' /tmp/dept-oidc-config.json)"
  echo "RESULT_DEPT_${dept}_CLIENT_ID=${dept_client_id}"
  echo "RESULT_DEPT_${dept}_CLIENT_SECRET=${dept_client_secret}"
done
