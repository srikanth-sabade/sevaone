CREATE TABLE orchestration.operator_wallet_txn (
    id uuid NOT NULL,
    operator_id uuid NOT NULL,
    citizen_id uuid NOT NULL,
    department_id uuid,
    scheme_id uuid,
    amount numeric(18,2) NOT NULL,
    txn_status character varying(30) NOT NULL,
    sevaone_token_id uuid,
    idempotency_key character varying(100),
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_operator_wallet_txn PRIMARY KEY (id),
    CONSTRAINT ck_operator_wallet_txn_amount_positive CHECK (amount > 0),
    CONSTRAINT fk_operator_wallet_txn_department FOREIGN KEY (department_id) REFERENCES discovery.department_master (id) ON DELETE RESTRICT,
    CONSTRAINT fk_operator_wallet_txn_operator FOREIGN KEY (operator_id) REFERENCES portal.operator_master (id) ON DELETE RESTRICT,
    CONSTRAINT fk_operator_wallet_txn_scheme FOREIGN KEY (scheme_id) REFERENCES discovery.sevaone_service_master (id) ON DELETE RESTRICT,
    CONSTRAINT fk_operator_wallet_txn_token FOREIGN KEY (sevaone_token_id) REFERENCES portal.sevaone_token (id) ON DELETE SET NULL,
    CONSTRAINT fk_operator_wallet_txn_citizen FOREIGN KEY (citizen_id) REFERENCES portal.sevaone_user (id) ON DELETE RESTRICT
);
CREATE INDEX ix_operator_wallet_txn_citizen_id ON orchestration.operator_wallet_txn (citizen_id);
CREATE INDEX ix_operator_wallet_txn_department_id ON orchestration.operator_wallet_txn (department_id);
CREATE UNIQUE INDEX ix_operator_wallet_txn_idempotency_key ON orchestration.operator_wallet_txn (idempotency_key) WHERE idempotency_key IS NOT NULL;
CREATE INDEX ix_operator_wallet_txn_operator_id ON orchestration.operator_wallet_txn (operator_id);
CREATE INDEX ix_operator_wallet_txn_scheme_id ON orchestration.operator_wallet_txn (scheme_id);
CREATE INDEX ix_operator_wallet_txn_token_id ON orchestration.operator_wallet_txn (sevaone_token_id);
