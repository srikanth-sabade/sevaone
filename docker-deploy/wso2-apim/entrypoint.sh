#!/bin/sh
# Wraps the STOCK wso2/wso2am entrypoint (bind-mounted into the container and set
# as its `entrypoint:` in docker-compose -- no custom image build at all) to import
# the WSO2 IS container's TLS certificate into APIM's trust store before starting
# APIM. Done at container startup (not image build time) because IS's self-signed
# cert is only known once that container is actually running -- baking it in at
# build time would require cross-image coupling and go stale the moment IS's cert
# is regenerated. See backend/docs/WSO2_IS_APIM_SETUP.md.
#
# After the cert import, hands off to the STOCK docker-entrypoint.sh (not
# api-manager.sh directly) so its own wso2-config-volume/wso2-artifact-volume
# copy logic still runs before the server starts -- that's how our mounted
# deployment.toml and the Postgres JDBC driver actually reach the product home.
set -eu

AM_HOME="$(ls -d /home/wso2carbon/wso2am-* 2>/dev/null | head -1)"
if [ -z "${AM_HOME}" ]; then
  echo "Could not locate WSO2 APIM home under /home/wso2carbon" >&2
  exit 1
fi
TRUSTSTORE="${AM_HOME}/repository/resources/security/client-truststore.jks"
STOREPASS="${TRUSTSTORE_PASSWORD:-wso2carbon}"
IS_HOST="${WSO2_IS_HOST:-wso2is}"
IS_PORT="${WSO2_IS_PORT:-9443}"

echo "Waiting for WSO2 IS at ${IS_HOST}:${IS_PORT} ..."
i=0
until echo | openssl s_client -connect "${IS_HOST}:${IS_PORT}" -servername "${IS_HOST}" 2>/dev/null | grep -q "BEGIN CERTIFICATE"; do
  i=$((i + 1))
  if [ "${i}" -ge 60 ]; then
    echo "WSO2 IS did not become reachable in time; starting APIM without cert import." >&2
    break
  fi
  sleep 5
done

CERT_FILE="/tmp/wso2is-cert.pem"
if echo | openssl s_client -connect "${IS_HOST}:${IS_PORT}" -servername "${IS_HOST}" 2>/dev/null \
    | openssl x509 -outform PEM > "${CERT_FILE}" 2>/dev/null && [ -s "${CERT_FILE}" ]; then
  if keytool -list -keystore "${TRUSTSTORE}" -storepass "${STOREPASS}" -alias wso2is-cert >/dev/null 2>&1; then
    echo "WSO2 IS cert already trusted."
  else
    keytool -importcert -alias wso2is-cert -keystore "${TRUSTSTORE}" -storepass "${STOREPASS}" -file "${CERT_FILE}" -noprompt
    echo "Imported WSO2 IS certificate into APIM client-truststore.jks"
  fi
else
  echo "Could not fetch WSO2 IS certificate; APIM will fail to call IS over TLS until this is fixed." >&2
fi

exec /home/wso2carbon/docker-entrypoint.sh
