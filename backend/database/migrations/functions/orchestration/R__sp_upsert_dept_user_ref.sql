-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION orchestration.sp_upsert_dept_user_ref(
    p_id uuid, p_department_id uuid, p_citizen_id uuid, p_operator_id uuid,
    p_assisted_mode boolean, p_created_at timestamptz
)
RETURNS TABLE (success boolean, message text)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO orchestration.dept_user_ref (id, department_id, citizen_id, operator_id, assisted_mode, created_at)
    VALUES (p_id, p_department_id, p_citizen_id, p_operator_id, p_assisted_mode, p_created_at)
    ON CONFLICT (department_id, citizen_id) DO NOTHING;
    RETURN QUERY SELECT TRUE, 'Department user reference recorded.'::text;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('orchestration.sp_upsert_dept_user_ref', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
