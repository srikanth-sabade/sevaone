-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- Append-only -- there is no update/delete function for this table on purpose, same convention
-- as discovery.sp_insert_service_change_log.

CREATE OR REPLACE FUNCTION portal.sp_insert_gov_admin_activity(
    p_id uuid, p_actor_username character varying(100), p_actor_display_name character varying(200),
    p_action character varying(64), p_target text, p_detail text
)
RETURNS TABLE ("Id" uuid)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    INSERT INTO portal.gov_admin_activity_log (id, actor_username, actor_display_name, action, target, detail)
    VALUES (p_id, p_actor_username, p_actor_display_name, p_action, p_target, p_detail)
    RETURNING id;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('portal.sp_insert_gov_admin_activity', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
