-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

-- Supersedes the earlier feature/stored-procedures pilot's sp_try_redeem_token (never
-- merged to develop) -- same atomic UPDATE ... WHERE revoked_at IS NULL guarantee, now
-- returning the same (success, message) envelope as the other write functions here
-- instead of a bare boolean, for consistency. The explicit DROP is required because
-- Postgres refuses CREATE OR REPLACE when the return type changes (boolean -> TABLE),
-- which is exactly what happened on any environment the pilot branch was ever run against.
DROP FUNCTION IF EXISTS portal.sp_try_redeem_token(uuid, timestamptz);

CREATE OR REPLACE FUNCTION portal.sp_try_redeem_token(p_token_id uuid, p_redeemed_at timestamptz)
RETURNS TABLE (success boolean, message text)
LANGUAGE plpgsql AS $$
DECLARE
    v_rows integer;
BEGIN
    UPDATE portal.sevaone_token
    SET revoked_at = p_redeemed_at, is_active = FALSE
    WHERE id = p_token_id AND revoked_at IS NULL;

    GET DIAGNOSTICS v_rows = ROW_COUNT;
    RETURN QUERY SELECT (v_rows = 1), (CASE WHEN v_rows = 1 THEN 'Token redeemed.' ELSE 'Token already redeemed or not found.' END)::text;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('portal.sp_try_redeem_token', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
