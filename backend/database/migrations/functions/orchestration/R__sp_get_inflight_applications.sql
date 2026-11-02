-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION orchestration.sp_get_inflight_applications(p_batch_size integer)
RETURNS TABLE (
    "Id" uuid, "CitizenId" uuid, "SevaReference" character varying(32), "DepartmentReference" text,
    "DepartmentCode" character varying(64), "Status" integer
)
LANGUAGE sql STABLE AS $$
    SELECT a.id, a.citizen_id, a.seva_reference, a.department_reference, d.department_id, a.status
    FROM orchestration.applications a
    JOIN discovery.department_master d ON d.id = a.department_id
    WHERE a.department_reference IS NOT NULL AND a.status NOT IN (3, 4) -- Approved, Rejected
      -- Token-integration applications are checked by their own status API (discovery.token_integration), not a connector.
      AND NOT EXISTS (SELECT 1 FROM discovery.token_integration_attempt t WHERE t.edcs_application_ref_no = a.seva_reference)
    LIMIT p_batch_size;
$$;
