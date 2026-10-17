-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

-- Explicit DROP: return row shape changed (V22/V25 -- department_master url column; V32 --
-- hierarchy_level/parent_department_id), which Postgres refuses to apply via CREATE OR REPLACE
-- alone.
DROP FUNCTION IF EXISTS discovery.sp_list_departments_with_scheme_count();

CREATE OR REPLACE FUNCTION discovery.sp_list_departments_with_scheme_count()
RETURNS TABLE (
    "Id" uuid, "Code" character varying(64), "Name" character varying(200), "PortalName" text,
    "PortalDomain" text, "IsIntegrated" boolean, "SchemeCount" bigint
)
LANGUAGE sql STABLE AS $$
    SELECT d.id, d.department_id, d.department_name, d.portal_name, d.portal_domain, d.is_integrated,
           count(s.id)
    FROM discovery.department_master d
    LEFT JOIN discovery.sevaone_service_master s ON s.department_id = d.id AND s.is_active = TRUE
    WHERE d.is_active = TRUE AND d.hierarchy_level = 1
    GROUP BY d.id, d.department_id, d.department_name, d.portal_name, d.portal_domain, d.is_integrated
    ORDER BY d.department_name;
$$;
