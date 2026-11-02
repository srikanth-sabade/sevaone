-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION orchestration.sp_get_application_by_reference(p_seva_reference character varying(32))
RETURNS SETOF orchestration.applications
LANGUAGE sql STABLE AS $$
    SELECT * FROM orchestration.applications WHERE seva_reference = p_seva_reference;
$$;
