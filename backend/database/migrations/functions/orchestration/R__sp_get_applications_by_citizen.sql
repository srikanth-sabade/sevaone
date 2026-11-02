-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION orchestration.sp_get_applications_by_citizen(p_citizen_id uuid)
RETURNS SETOF orchestration.applications
LANGUAGE sql STABLE AS $$
    SELECT * FROM orchestration.applications WHERE citizen_id = p_citizen_id ORDER BY submitted_at DESC;
$$;
