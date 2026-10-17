-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION orchestration.sp_expire_department_session(p_id uuid, p_expires_at timestamptz)
RETURNS TABLE (success boolean, message text)
LANGUAGE plpgsql AS $$
DECLARE
    v_rows integer;
BEGIN
    UPDATE orchestration.department_sessions SET expires_at = p_expires_at WHERE id = p_id;
    GET DIAGNOSTICS v_rows = ROW_COUNT;
    RETURN QUERY SELECT (v_rows = 1), (CASE WHEN v_rows = 1 THEN 'Session expired.' ELSE 'Session not found.' END)::text;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('orchestration.sp_expire_department_session', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
