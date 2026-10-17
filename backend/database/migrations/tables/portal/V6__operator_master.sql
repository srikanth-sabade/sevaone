CREATE TABLE portal.operator_master (
    id uuid NOT NULL,
    operator_id character varying(50) NOT NULL,
    operator_type character varying(10) NOT NULL,
    operator_name character varying(200),
    mobile_no character varying(15),
    wallet_balance numeric(18,2) NOT NULL DEFAULT 0.0,
    -- Operators have no WSO2 IS account -- SevaOne owns this credential directly (PBKDF2-SHA256,
    -- see OperatorPasswordHasher). Nullable: a seeded/legacy operator row with no password set
    -- stays non-loginable until one is, rather than the migration inventing one for it.
    password_hash text,
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_operator_master PRIMARY KEY (id),
    CONSTRAINT ck_operator_master_wallet_balance_non_negative CHECK (wallet_balance >= 0),
    -- Must match SevaOne.Domain.Enums.OperatorType exactly (G1/K1/B1, kiosk operator classes
    -- only -- NOT the frontend's broader assistedChannels list, which also has AJSK/BSK).
    CONSTRAINT ck_operator_master_operator_type CHECK (operator_type IN ('G1', 'K1', 'B1'))
);
CREATE UNIQUE INDEX ix_operator_master_operator_code ON portal.operator_master (operator_id);
