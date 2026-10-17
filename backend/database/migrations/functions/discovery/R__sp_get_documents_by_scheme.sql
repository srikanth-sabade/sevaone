-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION discovery.sp_get_documents_by_scheme(p_scheme_id uuid)
RETURNS SETOF discovery.sevaone_service_document
LANGUAGE sql STABLE AS $$
    SELECT * FROM discovery.sevaone_service_document WHERE scheme_id = p_scheme_id AND is_active = TRUE;
$$;
