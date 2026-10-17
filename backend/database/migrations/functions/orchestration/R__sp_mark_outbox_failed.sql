-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION orchestration.sp_mark_outbox_failed(p_id uuid, p_attempts integer, p_error text)
RETURNS TABLE (success boolean, message text)
LANGUAGE plpgsql AS $$
DECLARE
    v_rows integer;
BEGIN
    UPDATE orchestration.outbox_messages SET attempts = p_attempts, last_error = p_error WHERE id = p_id;
    GET DIAGNOSTICS v_rows = ROW_COUNT;
    RETURN QUERY SELECT (v_rows = 1), (CASE WHEN v_rows = 1 THEN 'Marked failed.' ELSE 'Outbox message not found.' END)::text;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('orchestration.sp_mark_outbox_failed', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
