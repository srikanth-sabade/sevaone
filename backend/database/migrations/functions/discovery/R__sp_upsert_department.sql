-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- Deliberately narrow: catalog ingestion only knows a department's name and its own public
-- URL. On first insert, portal_name/portal_domain/connector_type/is_integrated get sensible
-- placeholders (a brand-new department has no connector wiring yet -- that's set up by a
-- separate, human process). On conflict, those integration fields are left untouched --
-- ingestion must never overwrite curated connector config with blanks.

CREATE OR REPLACE FUNCTION discovery.sp_upsert_department(
    p_id uuid, p_department_id character varying(64), p_department_name character varying(200),
    p_department_url text, p_department_url_kannada text
)
RETURNS TABLE ("Id" uuid)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    INSERT INTO discovery.department_master (
        id, department_id, department_name, portal_name, portal_domain, department_url,
        department_url_kannada, connector_type, is_integrated, is_active
    )
    VALUES (
        p_id, p_department_id, p_department_name, p_department_name, '', p_department_url,
        p_department_url_kannada, 'generic', FALSE, TRUE
    )
    ON CONFLICT (department_id) DO UPDATE SET
        department_name = EXCLUDED.department_name,
        department_url = EXCLUDED.department_url,
        department_url_kannada = EXCLUDED.department_url_kannada
    RETURNING id;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('discovery.sp_upsert_department', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
