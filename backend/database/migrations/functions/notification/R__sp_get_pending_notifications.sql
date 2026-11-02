-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION notification.sp_get_pending_notifications(p_batch_size integer)
RETURNS SETOF notification.notifications
LANGUAGE sql STABLE AS $$
    SELECT * FROM notification.notifications WHERE sent_at IS NULL ORDER BY created_at LIMIT p_batch_size;
$$;
