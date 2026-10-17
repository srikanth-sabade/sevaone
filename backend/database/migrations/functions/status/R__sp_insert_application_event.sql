-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION status.sp_insert_application_event(
    p_id uuid, p_application_id uuid, p_status integer, p_note text, p_at timestamptz
)
RETURNS TABLE (success boolean, message text)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO status.application_events (id, application_id, status, note, at)
    VALUES (p_id, p_application_id, p_status, p_note, p_at);
    RETURN QUERY SELECT TRUE, 'Event recorded.'::text;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('status.sp_insert_application_event', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
