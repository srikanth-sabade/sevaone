-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- Single grant/revoke primitive -- the bulk "apply to every service in a department" admin
-- action loops this at the business layer rather than a SQL-side loop, matching this
-- schema's existing one-thing-per-function granularity.

CREATE OR REPLACE FUNCTION discovery.sp_set_service_visibility(
    p_service_id uuid, p_role_id uuid, p_grant boolean, p_granted_by text
)
RETURNS TABLE (success boolean)
LANGUAGE plpgsql AS $$
BEGIN
    IF p_grant THEN
        INSERT INTO discovery.service_role_visibility (service_id, role_id, granted_by)
        VALUES (p_service_id, p_role_id, p_granted_by)
        ON CONFLICT (service_id, role_id) DO UPDATE SET
            granted_at = now(),
            granted_by = EXCLUDED.granted_by;
    ELSE
        DELETE FROM discovery.service_role_visibility WHERE service_id = p_service_id AND role_id = p_role_id;
    END IF;
    RETURN QUERY SELECT TRUE;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('discovery.sp_set_service_visibility', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
