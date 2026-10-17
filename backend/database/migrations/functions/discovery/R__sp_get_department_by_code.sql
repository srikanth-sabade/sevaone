-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

-- Explicit DROP: return row shape changed (V52 -- restore department_url/department_url_kannada
-- columns), which Postgres refuses to apply via CREATE OR REPLACE alone.
DROP FUNCTION IF EXISTS discovery.sp_get_department_by_code(character varying);

CREATE OR REPLACE FUNCTION discovery.sp_get_department_by_code(p_code character varying(64))
RETURNS TABLE (
    "Id" uuid, "Code" character varying(64), "Name" character varying(200), "PortalName" text,
    "PortalDomain" text, "ConnectorType" text, "IsIntegrated" boolean,
    "DepartmentUrl" text, "DepartmentUrlKannada" text,
    "IsActive" boolean, "CreatedAt" timestamp with time zone
)
LANGUAGE sql STABLE AS $$
    SELECT id, department_id, department_name, portal_name, portal_domain, connector_type,
           is_integrated, department_url, department_url_kannada, is_active, created_at
    FROM discovery.department_master
    WHERE department_id = p_code;
$$;
