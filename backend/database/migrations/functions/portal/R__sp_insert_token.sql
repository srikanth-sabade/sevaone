-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

-- Explicit DROP required because the parameter list is changing (added p_return_url) --
-- Postgres refuses CREATE OR REPLACE when the signature changes, same reason
-- sp_try_redeem_token needed one earlier (see that file's comment).
DROP FUNCTION IF EXISTS portal.sp_insert_token(
    uuid, uuid, bigint, uuid, boolean, uuid, uuid, text, text, timestamptz, timestamptz, timestamptz, timestamptz, boolean
);

CREATE OR REPLACE FUNCTION portal.sp_insert_token(
    p_id uuid, p_citizen_id uuid, p_sevaone_user_id bigint, p_operator_id uuid,
    p_assisted_mode boolean, p_department_id uuid, p_scheme_id uuid, p_token_value text,
    p_token_hash text, p_issued_at timestamptz, p_expire_at timestamptz,
    p_midnight_valid_until timestamptz, p_revoked_at timestamptz, p_is_active boolean,
    p_return_url text
)
RETURNS TABLE (success boolean, message text)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO portal.sevaone_token (
        id, citizen_id, sevaone_user_id, operator_id, assisted_mode, department_id, scheme_id,
        token_value, token_hash, issued_at, expire_at, midnight_valid_until, revoked_at, is_active,
        return_url
    ) VALUES (
        p_id, p_citizen_id, p_sevaone_user_id, p_operator_id, p_assisted_mode, p_department_id, p_scheme_id,
        p_token_value, p_token_hash, p_issued_at, p_expire_at, p_midnight_valid_until, p_revoked_at, p_is_active,
        p_return_url
    );
    RETURN QUERY SELECT TRUE, 'Token issued.'::text;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('portal.sp_insert_token', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
