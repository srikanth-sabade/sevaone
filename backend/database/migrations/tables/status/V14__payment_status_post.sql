CREATE TABLE status.payment_status_post (
    id uuid NOT NULL,
    citizen_id uuid,
    department_id uuid,
    scheme_id uuid,
    sevaone_token_id uuid,
    token_value text NOT NULL,
    token_hash_at_post character varying(64) NOT NULL,
    payment_status character varying(50) NOT NULL,
    amount numeric(18,2),
    posted_at timestamp with time zone NOT NULL,
    token_valid_till timestamp with time zone NOT NULL,
    idempotency_key character varying(100),
    CONSTRAINT pk_payment_status_post PRIMARY KEY (id),
    CONSTRAINT ck_payment_status_post_amount_non_negative CHECK (amount IS NULL OR amount >= 0),
    CONSTRAINT fk_payment_status_post_department FOREIGN KEY (department_id) REFERENCES discovery.department_master (id) ON DELETE RESTRICT,
    CONSTRAINT fk_payment_status_post_scheme FOREIGN KEY (scheme_id) REFERENCES discovery.sevaone_service_master (id) ON DELETE RESTRICT,
    CONSTRAINT fk_payment_status_post_token FOREIGN KEY (sevaone_token_id) REFERENCES portal.sevaone_token (id) ON DELETE SET NULL,
    CONSTRAINT fk_payment_status_post_citizen FOREIGN KEY (citizen_id) REFERENCES portal.sevaone_user (id) ON DELETE RESTRICT
);
CREATE INDEX ix_payment_status_post_citizen_id ON status.payment_status_post (citizen_id);
CREATE INDEX ix_payment_status_post_department_id ON status.payment_status_post (department_id);
CREATE UNIQUE INDEX ix_payment_status_post_idempotency_key ON status.payment_status_post (idempotency_key) WHERE idempotency_key IS NOT NULL;
CREATE INDEX ix_payment_status_post_posted_at ON status.payment_status_post (posted_at);
CREATE INDEX ix_payment_status_post_scheme_id ON status.payment_status_post (scheme_id);
CREATE INDEX ix_payment_status_post_token_id ON status.payment_status_post (sevaone_token_id);
