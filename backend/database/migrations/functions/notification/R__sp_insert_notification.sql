-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION notification.sp_insert_notification(
    p_id uuid, p_citizen_id uuid, p_channel text, p_recipient text, p_subject text,
    p_body text, p_created_at timestamptz, p_sent_at timestamptz
)
RETURNS TABLE (success boolean, message text)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO notification.notifications (id, citizen_id, channel, recipient, subject, body, created_at, sent_at)
    VALUES (p_id, p_citizen_id, p_channel, p_recipient, p_subject, p_body, p_created_at, p_sent_at);
    RETURN QUERY SELECT TRUE, 'Notification queued.'::text;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('notification.sp_insert_notification', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
