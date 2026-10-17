-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- p_service_id filters to one service's history when given; NULL returns the whole
-- catalogue's audit log (most-recent first), capped at p_limit.

CREATE OR REPLACE FUNCTION discovery.sp_list_service_change_log(p_service_id uuid, p_limit integer)
RETURNS TABLE (
    "Id" uuid, "ServiceId" uuid, "BatchId" uuid, "ChangedAt" timestamptz, "ChangedBy" text,
    "ChangeType" character varying(32), "Diff" text
)
LANGUAGE sql STABLE AS $$
    SELECT id, service_id, batch_id, changed_at, changed_by, change_type, diff
    FROM discovery.service_change_log
    WHERE p_service_id IS NULL OR service_id = p_service_id
    ORDER BY changed_at DESC
    LIMIT p_limit;
$$;
