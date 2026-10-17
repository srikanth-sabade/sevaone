-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- Append-only -- there is no update/delete function for this table on purpose. Written by
-- publish (created/updated), manual single-service edits, and visibility grants/revokes
-- alike, not just imports.

CREATE OR REPLACE FUNCTION discovery.sp_insert_service_change_log(
    p_id uuid, p_service_id uuid, p_batch_id uuid, p_changed_by text,
    p_change_type character varying(32), p_diff text
)
RETURNS TABLE ("Id" uuid)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    INSERT INTO discovery.service_change_log (id, service_id, batch_id, changed_by, change_type, diff)
    VALUES (p_id, p_service_id, p_batch_id, p_changed_by, p_change_type, p_diff)
    RETURNING id;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('discovery.sp_insert_service_change_log', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
