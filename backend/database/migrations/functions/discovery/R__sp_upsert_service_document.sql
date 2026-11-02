-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- Natural key is (scheme_id, document_url) -- see V48. A source spreadsheet's "Required
-- Documents English/Kannada" land here, not as another documents_json blob (V24's own
-- comment on sevaone_service_document says as much).

CREATE OR REPLACE FUNCTION discovery.sp_upsert_service_document(
    p_id uuid, p_scheme_id uuid, p_document_name character varying(200),
    p_document_name_kannada character varying(200), p_document_url character varying(500),
    p_document_type character varying(50), p_is_mandatory boolean
)
RETURNS TABLE ("Id" uuid)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    INSERT INTO discovery.sevaone_service_document (
        id, scheme_id, document_name, document_name_kannada, document_url, document_type,
        is_mandatory, is_active
    )
    VALUES (
        p_id, p_scheme_id, p_document_name, p_document_name_kannada, p_document_url, p_document_type,
        p_is_mandatory, TRUE
    )
    ON CONFLICT (scheme_id, document_url) DO UPDATE SET
        document_name = EXCLUDED.document_name,
        document_name_kannada = EXCLUDED.document_name_kannada,
        document_type = EXCLUDED.document_type,
        is_mandatory = EXCLUDED.is_mandatory,
        is_active = TRUE
    RETURNING id;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('discovery.sp_upsert_service_document', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
