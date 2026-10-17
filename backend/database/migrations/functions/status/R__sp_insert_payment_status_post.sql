-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

-- Write is a plain insert (no conditional/atomic logic needed -- idempotency is enforced by
-- ix_payment_status_post_idempotency_key), unlike portal's wallet/token functions.
CREATE OR REPLACE FUNCTION status.sp_insert_payment_status_post(
    p_id uuid, p_citizen_id uuid, p_department_id uuid, p_scheme_id uuid, p_sevaone_token_id uuid,
    p_token_value text, p_token_hash_at_post character varying(64), p_payment_status character varying(50),
    p_amount numeric, p_posted_at timestamptz, p_token_valid_till timestamptz, p_idempotency_key character varying(100)
)
RETURNS TABLE (success boolean, message text)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO status.payment_status_post
        (id, citizen_id, department_id, scheme_id, sevaone_token_id, token_value, token_hash_at_post,
         payment_status, amount, posted_at, token_valid_till, idempotency_key)
    VALUES
        (p_id, p_citizen_id, p_department_id, p_scheme_id, p_sevaone_token_id, p_token_value, p_token_hash_at_post,
         p_payment_status, p_amount, p_posted_at, p_token_valid_till, p_idempotency_key);
    RETURN QUERY SELECT TRUE, 'Payment status posted.'::text;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('status.sp_insert_payment_status_post', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
