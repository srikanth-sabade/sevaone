-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION status.sp_get_timeline(p_application_id uuid)
RETURNS SETOF status.application_events
LANGUAGE sql STABLE AS $$
    SELECT * FROM status.application_events WHERE application_id = p_application_id ORDER BY at;
$$;
