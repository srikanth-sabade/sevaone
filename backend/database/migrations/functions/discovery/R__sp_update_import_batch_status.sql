-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- p_row_count/error_count/warning_count are nullable -- pass NULL to leave a count
-- unchanged (e.g. a publish/discard transition that only needs to flip status).

CREATE OR REPLACE FUNCTION discovery.sp_update_import_batch_status(
    p_id uuid, p_status character varying(32), p_row_count integer, p_error_count integer,
    p_warning_count integer, p_published_by text
)
RETURNS TABLE (success boolean)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE discovery.catalog_import_batch SET
        status = p_status,
        row_count = COALESCE(p_row_count, row_count),
        error_count = COALESCE(p_error_count, error_count),
        warning_count = COALESCE(p_warning_count, warning_count),
        published_at = CASE WHEN p_status = 'published' THEN now() ELSE published_at END,
        published_by = CASE WHEN p_status = 'published' THEN p_published_by ELSE published_by END
    WHERE id = p_id;
    RETURN QUERY SELECT TRUE;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('discovery.sp_update_import_batch_status', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
