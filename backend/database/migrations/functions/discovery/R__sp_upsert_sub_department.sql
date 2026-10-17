-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- display_order is deliberately not a parameter here -- it's admin-controlled ordering,
-- untouched by re-imports. A conflict just reactivates the sub-department if it had been
-- deactivated (is_active = FALSE) since the last import.

CREATE OR REPLACE FUNCTION discovery.sp_upsert_sub_department(
    p_id uuid, p_department_id uuid, p_sub_department_name character varying(200)
)
RETURNS TABLE ("Id" uuid)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    INSERT INTO discovery.sub_department_master (id, department_id, sub_department_name, is_active)
    VALUES (p_id, p_department_id, p_sub_department_name, TRUE)
    ON CONFLICT (department_id, sub_department_name) DO UPDATE SET
        is_active = TRUE
    RETURNING id;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('discovery.sp_upsert_sub_department', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
