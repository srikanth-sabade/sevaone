-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

-- Explicit DROP: return row shape changed (V22/V25 -- department_master url column; V32 --
-- hierarchy_level/parent_department_id), which Postgres refuses to apply via CREATE OR REPLACE
-- alone.
DROP FUNCTION IF EXISTS discovery.sp_list_departments();

CREATE OR REPLACE FUNCTION discovery.sp_list_departments()
RETURNS TABLE (
    "Id" uuid, "Code" character varying(64), "Name" character varying(200), "PortalName" text,
    "PortalDomain" text, "ConnectorType" text, "IsIntegrated" boolean, "IsActive" boolean,
    "CreatedAt" timestamp with time zone
)
LANGUAGE sql STABLE AS $$
    SELECT id, department_id, department_name, portal_name, portal_domain, connector_type,
           is_integrated, is_active, created_at
    FROM discovery.department_master
    WHERE is_active = TRUE AND hierarchy_level = 1
    ORDER BY department_name;
$$;
