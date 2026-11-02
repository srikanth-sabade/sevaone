-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION orchestration.sp_get_pending_outbox_messages(p_max_attempts integer, p_batch_size integer)
RETURNS SETOF orchestration.outbox_messages
LANGUAGE sql STABLE AS $$
    SELECT * FROM orchestration.outbox_messages
    WHERE published_at IS NULL AND attempts < p_max_attempts
    ORDER BY created_at
    LIMIT p_batch_size;
$$;
