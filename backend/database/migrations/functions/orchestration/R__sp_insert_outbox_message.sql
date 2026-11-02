-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION orchestration.sp_insert_outbox_message(
    p_id uuid, p_topic text, p_payload text, p_created_at timestamptz,
    p_published_at timestamptz, p_attempts integer, p_last_error text
)
RETURNS TABLE (success boolean, message text)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO orchestration.outbox_messages (id, topic, payload, created_at, published_at, attempts, last_error)
    VALUES (p_id, p_topic, p_payload, p_created_at, p_published_at, p_attempts, p_last_error);
    RETURN QUERY SELECT TRUE, 'Outbox message queued.'::text;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('orchestration.sp_insert_outbox_message', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
