-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION orchestration.sp_get_application_by_department_reference(
    p_department_code text, p_department_reference text
)
RETURNS SETOF orchestration.applications
LANGUAGE sql STABLE AS $$
    SELECT a.* FROM orchestration.applications a
    JOIN discovery.department_master d ON d.id = a.department_id
    WHERE a.department_reference = p_department_reference AND d.department_id = p_department_code;
$$;
