-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION discovery.sp_get_service_apply_count(p_scheme_id uuid)
RETURNS bigint
LANGUAGE sql STABLE AS $$
    SELECT count(*) FROM discovery.service_apply_track WHERE scheme_id = p_scheme_id;
$$;
