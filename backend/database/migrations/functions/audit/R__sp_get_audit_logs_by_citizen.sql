-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION audit.sp_get_audit_logs_by_citizen(p_citizen_id uuid, p_take integer)
RETURNS SETOF audit.audit_logs
LANGUAGE sql STABLE AS $$
    SELECT * FROM audit.audit_logs WHERE citizen_id = p_citizen_id ORDER BY at DESC LIMIT p_take;
$$;
