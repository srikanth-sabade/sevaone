-- Status check (contract URL 3): we ask the department for an application's status.
-- The contract only says "a status-check API using the GSC number", so how to read the reply is
-- set per service (token_integration.extra_config -> "status": path to the status text, optional
-- message path, and a map from the department's words to ours). The secret for calling the API
-- (when it needs one) is kept encrypted in status_auth_secret_ciphertext and never read back.

ALTER TABLE discovery.token_integration ADD COLUMN status_auth_secret_ciphertext text;

ALTER TABLE discovery.token_integration_attempt
    ADD COLUMN status_checked_at timestamp with time zone,
    ADD COLUMN department_status_text varchar(200),
    ADD COLUMN status_check_error varchar(300);

CREATE INDEX ix_token_integration_attempt_status_due ON discovery.token_integration_attempt (status_checked_at NULLS FIRST)
    WHERE status = 'acknowledged';
