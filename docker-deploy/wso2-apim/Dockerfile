FROM wso2/wso2am:4.5.0

USER root
COPY entrypoint.sh /usr/local/bin/seva-apim-entrypoint.sh
COPY idp-overrides.toml /tmp/idp-overrides.toml
RUN set -eux; \
    chmod +x /usr/local/bin/seva-apim-entrypoint.sh; \
    AM_HOME="$(ls -d /home/wso2carbon/wso2am-* 2>/dev/null | head -1)"; \
    test -n "$AM_HOME"; \
    TOML="$AM_HOME/repository/conf/deployment.toml"; \
    if ! grep -q 'seva: correct APIM self-referenced login URLs' "$TOML"; then \
      printf '\n# --- seva: correct APIM self-referenced login URLs for the Docker port mapping ---\n' >> "$TOML"; \
      cat /tmp/idp-overrides.toml >> "$TOML"; \
    fi; \
    grep -q '^server_url = "https://localhost:9444"$' "$TOML"; \
    grep -q '^proxyPort = 9444$' "$TOML"
USER wso2carbon

ENTRYPOINT ["/usr/local/bin/seva-apim-entrypoint.sh"]
