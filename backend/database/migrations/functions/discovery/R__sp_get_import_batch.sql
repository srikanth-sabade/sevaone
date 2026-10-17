-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION discovery.sp_get_import_batch(p_id uuid)
RETURNS TABLE (
    "Id" uuid, "SourceFilename" character varying(500), "SourceFormat" character varying(16),
    "UploadedBy" text, "UploadedAt" timestamptz, "Status" character varying(32),
    "RowCount" integer, "ErrorCount" integer, "WarningCount" integer,
    "PublishedAt" timestamptz, "PublishedBy" text
)
LANGUAGE sql STABLE AS $$
    SELECT id, source_filename, source_format, uploaded_by, uploaded_at, status,
           row_count, error_count, warning_count, published_at, published_by
    FROM discovery.catalog_import_batch
    WHERE id = p_id;
$$;
