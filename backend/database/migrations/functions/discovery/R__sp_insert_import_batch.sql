-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION discovery.sp_insert_import_batch(
    p_id uuid, p_source_filename character varying(500), p_source_format character varying(16),
    p_uploaded_by text
)
RETURNS TABLE ("Id" uuid)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    INSERT INTO discovery.catalog_import_batch (id, source_filename, source_format, uploaded_by, status)
    VALUES (p_id, p_source_filename, p_source_format, p_uploaded_by, 'validating')
    RETURNING id;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('discovery.sp_insert_import_batch', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
