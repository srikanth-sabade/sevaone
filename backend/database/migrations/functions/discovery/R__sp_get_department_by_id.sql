-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION discovery.sp_get_department_by_id(p_id uuid)
RETURNS TABLE (
    "Id" uuid, "Code" character varying(64), "Name" character varying(200), "PortalName" text,
    "PortalDomain" text, "ConnectorType" text, "IsIntegrated" boolean, "IsActive" boolean,
    "CreatedAt" timestamp with time zone
)
LANGUAGE sql STABLE AS $$
    SELECT id, department_id, department_name, portal_name, portal_domain, connector_type,
           is_integrated, is_active, created_at
    FROM discovery.department_master
    WHERE id = p_id;
$$;
