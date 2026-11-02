-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION discovery.sp_insert_service_apply_track(
    p_id uuid, p_scheme_id uuid, p_department_id uuid, p_citizen_id uuid,
    p_assisted_mode boolean, p_is_homepage_fallback boolean, p_created_at timestamptz,
    p_source character varying(16) DEFAULT 'catalogue'
)
RETURNS TABLE (success boolean, message text)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO discovery.service_apply_track (
        id, scheme_id, department_id, citizen_id, assisted_mode, is_homepage_fallback, created_at, source
    ) VALUES (
        p_id, p_scheme_id, p_department_id, p_citizen_id, p_assisted_mode, p_is_homepage_fallback, p_created_at, p_source
    );
    RETURN QUERY SELECT TRUE, 'Apply track recorded.'::text;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('discovery.sp_insert_service_apply_track', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
