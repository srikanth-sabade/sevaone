-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION orchestration.sp_get_wallet_txn_by_idempotency_key(p_idempotency_key character varying(100))
RETURNS SETOF orchestration.operator_wallet_txn
LANGUAGE sql STABLE AS $$
    SELECT * FROM orchestration.operator_wallet_txn WHERE idempotency_key = p_idempotency_key;
$$;
