-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION orchestration.sp_reassign_applications(p_from_citizen_id uuid, p_to_citizen_id uuid)
RETURNS TABLE (success boolean, message text)
LANGUAGE plpgsql AS $$
DECLARE
    v_rows integer;
BEGIN
    UPDATE orchestration.applications SET citizen_id = p_to_citizen_id WHERE citizen_id = p_from_citizen_id;
    GET DIAGNOSTICS v_rows = ROW_COUNT;
    RETURN QUERY SELECT TRUE, format('%s application(s) reassigned.', v_rows)::text;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('orchestration.sp_reassign_applications', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
