-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION orchestration.sp_insert_department_session(
    p_id uuid, p_citizen_id uuid, p_department_id uuid, p_encrypted_payload text,
    p_issued_at timestamptz, p_expires_at timestamptz
)
RETURNS TABLE (success boolean, message text)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO orchestration.department_sessions (id, citizen_id, department_id, encrypted_payload, issued_at, expires_at)
    VALUES (p_id, p_citizen_id, p_department_id, p_encrypted_payload, p_issued_at, p_expires_at);
    RETURN QUERY SELECT TRUE, 'Department session created.'::text;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('orchestration.sp_insert_department_session', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
