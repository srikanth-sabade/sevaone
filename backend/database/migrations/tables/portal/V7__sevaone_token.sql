-- One-time token redemption does NOT rely on Postgres xmin (that was the EF Core version's
-- optimistic-concurrency mechanism). Dapper has no change tracker, so redeem/revoke uses an
-- atomic UPDATE ... WHERE revoked_at IS NULL, checked via rows-affected == 1.
CREATE TABLE portal.sevaone_token (
    id uuid NOT NULL,
    citizen_id uuid NOT NULL,
    sevaone_user_id bigint NOT NULL,
    operator_id uuid,
    assisted_mode boolean NOT NULL DEFAULT FALSE,
    department_id uuid NOT NULL,
    scheme_id uuid NOT NULL,
    token_value text NOT NULL,
    token_hash character varying(64) NOT NULL,
    issued_at timestamp with time zone NOT NULL,
    expire_at timestamp with time zone NOT NULL,
    midnight_valid_until timestamp with time zone,
    revoked_at timestamp with time zone,
    is_active boolean NOT NULL DEFAULT TRUE,
    -- Where to send the citizen's browser back after the department-return-redirect flow.
    -- Nullable -- not every token (e.g. a Payment Status POST-only flow) has one.
    return_url text,
    CONSTRAINT pk_sevaone_token PRIMARY KEY (id),
    CONSTRAINT ck_sevaone_token_assisted_mode_operator CHECK ((assisted_mode = TRUE AND operator_id IS NOT NULL) OR (assisted_mode = FALSE AND operator_id IS NULL)),
    CONSTRAINT ck_sevaone_token_expiry_order CHECK (midnight_valid_until IS NULL OR midnight_valid_until >= expire_at),
    CONSTRAINT fk_sevaone_token_department FOREIGN KEY (department_id) REFERENCES discovery.department_master (id) ON DELETE RESTRICT,
    CONSTRAINT fk_sevaone_token_operator FOREIGN KEY (operator_id) REFERENCES portal.operator_master (id) ON DELETE RESTRICT,
    CONSTRAINT fk_sevaone_token_scheme FOREIGN KEY (scheme_id) REFERENCES discovery.sevaone_service_master (id) ON DELETE RESTRICT,
    CONSTRAINT fk_sevaone_token_citizen FOREIGN KEY (citizen_id) REFERENCES portal.sevaone_user (id) ON DELETE RESTRICT
);
CREATE INDEX ix_sevaone_token_citizen_id_is_active ON portal.sevaone_token (citizen_id, is_active);
CREATE INDEX ix_sevaone_token_department_id ON portal.sevaone_token (department_id);
CREATE INDEX ix_sevaone_token_expire_at ON portal.sevaone_token (expire_at);
CREATE INDEX ix_sevaone_token_operator_id ON portal.sevaone_token (operator_id);
CREATE INDEX ix_sevaone_token_scheme_id ON portal.sevaone_token (scheme_id);
CREATE INDEX ix_sevaone_token_sevaone_user_id ON portal.sevaone_token (sevaone_user_id);
CREATE UNIQUE INDEX ix_sevaone_token_token_hash ON portal.sevaone_token (token_hash);
