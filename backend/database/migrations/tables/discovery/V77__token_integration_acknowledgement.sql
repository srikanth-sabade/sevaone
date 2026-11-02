-- Acknowledgement API (contract URL 2): the department calls us after it has the application.
-- token_integration_credential: Basic-auth logins we issue to departments (password stored only as
--   a salted hash; shown once at creation). One department may hold several (rotation).
-- token_integration_update: every distinct acknowledgement received, so DepartmentApplicationUpdateId
--   in the reply is stable and a repeated identical call (the department retrying) is recognised.

ALTER TABLE discovery.token_integration_attempt ADD COLUMN token_verified boolean;

CREATE TABLE discovery.token_integration_credential (
    id uuid NOT NULL,
    department_id uuid NOT NULL,
    username varchar(80) NOT NULL,
    password_hash text NOT NULL,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    created_by text NOT NULL,
    password_changed_at timestamp with time zone NOT NULL DEFAULT now(),
    last_used_at timestamp with time zone,
    CONSTRAINT pk_token_integration_credential PRIMARY KEY (id),
    CONSTRAINT uq_token_integration_credential_username UNIQUE (username),
    CONSTRAINT fk_token_integration_credential_department FOREIGN KEY (department_id) REFERENCES discovery.department_master (id) ON DELETE CASCADE
);
CREATE INDEX ix_token_integration_credential_department ON discovery.token_integration_credential (department_id);

CREATE TABLE discovery.token_integration_update (
    id bigint GENERATED ALWAYS AS IDENTITY,
    attempt_id uuid NOT NULL,
    payload_hash char(64) NOT NULL,
    payload jsonb NOT NULL,
    caller_ip text,
    received_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_token_integration_update PRIMARY KEY (id),
    CONSTRAINT uq_token_integration_update_payload UNIQUE (attempt_id, payload_hash),
    CONSTRAINT fk_token_integration_update_attempt FOREIGN KEY (attempt_id) REFERENCES discovery.token_integration_attempt (id) ON DELETE CASCADE
);
