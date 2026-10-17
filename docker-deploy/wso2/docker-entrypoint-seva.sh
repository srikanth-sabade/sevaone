#!/bin/sh
# Wraps the stock WSO2 IS entrypoint with a one-time fixup for a real bug in
# the apim-km-wso2is connector (dropins/webapps installed by ../Dockerfile):
# its ReservedUserCreationObserver generates the "apim_reserved_user" service
# account's password from an UPPERCASE+digits-only charset. IS 7.3's default
# password policy requires a lowercase character, so that addUser call always
# fails with INM-60011 and the connector's OSGi bundle never activates --
# meaning /keymanager-operations/* (required by APIM) silently 404s forever.
#
# Fix: let IS boot once, pre-create that user ourselves with a compliant
# password via SCIM2, then restart the JVM in-place. On the second boot the
# connector's own isExistingUser() check short-circuits before it ever
# generates a password, and the bundle activates cleanly. See
# backend/docs/WSO2_IS_APIM_SETUP.md for the full writeup.
set -u

ORIG_ENTRYPOINT=/home/wso2carbon/docker-entrypoint.sh
RESERVED_USER=apim_reserved_user
RESERVED_PASSWORD="${APIM_RESERVED_USER_PASSWORD:-ApimReserved#2026}"
JAVA_MATCH="wso2is-7.3.0"

terminate() {
  pkill -TERM -f "${JAVA_MATCH}" 2>/dev/null || true
  exit 0
}
trap terminate TERM INT

start_is() {
  "${ORIG_ENTRYPOINT}" "$@" &
}

wait_reachable() {
  i=0
  until curl -skf https://localhost:9443/carbon/admin/login.jsp >/dev/null 2>&1; do
    i=$((i + 1))
    [ "${i}" -ge 120 ] && return 1
    sleep 3
  done
  return 0
}

start_is "$@"

if wait_reachable; then
  hit_count="$(curl -sk -u admin:admin \
    "https://localhost:9443/scim2/Users?filter=userName+eq+%22${RESERVED_USER}%22" \
    | grep -c '"userName"' || true)"
  if [ "${hit_count}" = "0" ]; then
    echo "seva: pre-creating ${RESERVED_USER} (works around the connector's uppercase-only password generator)"
    curl -sk -u admin:admin -H 'Content-Type: application/scim+json' \
      "https://localhost:9443/scim2/Users" \
      -d "{\"schemas\":[],\"userName\":\"${RESERVED_USER}\",\"password\":\"${RESERVED_PASSWORD}\"}" >/dev/null
    echo "seva: restarting WSO2 IS once so the key-manager connector picks up the pre-created user ..."
    pkill -TERM -f "${JAVA_MATCH}" 2>/dev/null || true
    j=0
    while pgrep -f "${JAVA_MATCH}" >/dev/null 2>&1; do
      j=$((j + 1))
      [ "${j}" -ge 40 ] && break
      sleep 3
    done
    start_is "$@"
  else
    echo "seva: ${RESERVED_USER} already present; connector should activate cleanly."
  fi
else
  echo "seva: WSO2 IS did not become reachable in time; skipping reserved-user fixup." >&2
fi

wait
