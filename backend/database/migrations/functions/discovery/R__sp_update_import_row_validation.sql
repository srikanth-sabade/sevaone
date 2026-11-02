-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- Second pass over an already-staged row (see sp_insert_import_row) once the validator has
-- decided its action and collected errors/warnings, including cross-row checks that need
-- every row in the batch present first (e.g. a duplicate composite code within the batch).

CREATE OR REPLACE FUNCTION discovery.sp_update_import_row_validation(
    p_id uuid, p_action character varying(16), p_validation_errors text, p_validation_warnings text
)
RETURNS TABLE (success boolean)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE discovery.catalog_import_row SET
        action = p_action,
        validation_errors = COALESCE(p_validation_errors, '[]'),
        validation_warnings = COALESCE(p_validation_warnings, '[]')
    WHERE id = p_id;
    RETURN QUERY SELECT TRUE;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('discovery.sp_update_import_row_validation', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
