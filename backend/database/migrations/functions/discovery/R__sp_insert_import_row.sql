-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- Called once per parsed service row during staging -- never touches the live catalog
-- tables. p_action/p_validation_errors/p_validation_warnings are typically computed by the
-- caller after this row and its siblings have all been staged (cross-row checks like
-- duplicate composite codes within the batch need the whole batch present first), so this
-- function only writes the row; a later pass updates action/errors/warnings in place.

CREATE OR REPLACE FUNCTION discovery.sp_insert_import_row(
    p_id uuid, p_batch_id uuid, p_row_number integer, p_raw_payload text, p_normalized_payload text
)
RETURNS TABLE ("Id" uuid)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    INSERT INTO discovery.catalog_import_row (id, batch_id, row_number, raw_payload, normalized_payload)
    VALUES (p_id, p_batch_id, p_row_number, p_raw_payload, p_normalized_payload)
    RETURNING id;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('discovery.sp_insert_import_row', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
