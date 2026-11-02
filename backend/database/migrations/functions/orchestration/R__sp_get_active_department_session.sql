-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION orchestration.sp_get_active_department_session(p_citizen_id uuid, p_department_id uuid)
RETURNS SETOF orchestration.department_sessions
LANGUAGE sql STABLE AS $$
    SELECT * FROM orchestration.department_sessions
    WHERE citizen_id = p_citizen_id AND department_id = p_department_id AND expires_at > now()
    ORDER BY expires_at DESC
    LIMIT 1;
$$;
