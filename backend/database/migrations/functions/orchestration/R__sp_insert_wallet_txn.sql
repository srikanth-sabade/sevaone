-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

-- Concurrency-sensitive in the same way as portal.sp_try_deduct_wallet: this only INSERTs a
-- transaction row (the balance mutation itself lives in portal.sp_try_deduct_wallet), so no
-- row-count-based conditional logic is needed here beyond the idempotency-key uniqueness
-- constraint already enforced by ix_operator_wallet_txn_idempotency_key.
CREATE OR REPLACE FUNCTION orchestration.sp_insert_wallet_txn(
    p_id uuid, p_operator_id uuid, p_citizen_id uuid, p_department_id uuid, p_scheme_id uuid,
    p_amount numeric, p_txn_status character varying(30), p_sevaone_token_id uuid,
    p_idempotency_key character varying(100), p_created_at timestamptz
)
RETURNS TABLE (success boolean, message text)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO orchestration.operator_wallet_txn
        (id, operator_id, citizen_id, department_id, scheme_id, amount, txn_status,
         sevaone_token_id, idempotency_key, created_at)
    VALUES
        (p_id, p_operator_id, p_citizen_id, p_department_id, p_scheme_id, p_amount, p_txn_status,
         p_sevaone_token_id, p_idempotency_key, p_created_at);
    RETURN QUERY SELECT TRUE, 'Wallet transaction recorded.'::text;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('orchestration.sp_insert_wallet_txn', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
