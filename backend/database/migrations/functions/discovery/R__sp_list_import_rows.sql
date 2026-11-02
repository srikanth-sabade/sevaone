-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- p_action filters to one action (insert/update/unchanged/conflict) when given; NULL
-- returns every row in the batch. This is the admin review surface -- never read by the
-- live citizen/operator catalog.

CREATE OR REPLACE FUNCTION discovery.sp_list_import_rows(p_batch_id uuid, p_action character varying(16))
RETURNS TABLE (
    "Id" uuid, "RowNumber" integer, "RawPayload" text, "NormalizedPayload" text,
    "Action" character varying(16), "ValidationErrors" text, "ValidationWarnings" text
)
LANGUAGE sql STABLE AS $$
    SELECT id, row_number, raw_payload, normalized_payload, action, validation_errors, validation_warnings
    FROM discovery.catalog_import_row
    WHERE batch_id = p_batch_id AND (p_action IS NULL OR action = p_action)
    ORDER BY row_number;
$$;
