-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- Landing-grid data for the Service Catalogue: every department, with a service_count that
-- only counts services actually visible to p_role_key (via service_role_visibility) --
-- never the department's true total. Existing sp_list_departments/sp_list_departments_with_
-- scheme_count are untouched (still back the older, unrelated ~40-service Departments.tsx
-- catalog with no role concept) -- this is the new, role-aware entrypoint for /catalog.

CREATE OR REPLACE FUNCTION discovery.sp_list_departments_for_role(p_role_key character varying(64))
RETURNS TABLE (
    "Id" uuid, "Code" character varying(64), "Name" character varying(200), "DepartmentUrl" text,
    "DepartmentUrlKannada" text, "SubDepartmentCount" bigint, "ServiceCount" bigint
)
LANGUAGE sql STABLE AS $$
    SELECT
        d.id, d.department_id, d.department_name, d.department_url, d.department_url_kannada,
        (SELECT COUNT(*) FROM discovery.sub_department_master sd WHERE sd.department_id = d.id AND sd.is_active),
        (SELECT COUNT(*) FROM discovery.sevaone_service_master s
            JOIN discovery.service_role_visibility v ON v.service_id = s.id
            JOIN discovery.catalog_role r ON r.id = v.role_id
            WHERE s.department_id = d.id AND s.is_active AND r.role_key = p_role_key)
    FROM discovery.department_master d
    WHERE d.is_active = TRUE
      -- A department whose every service is invisible to this role (an operator-only
      -- Sakala-token department such as BESCOM/CESCOM/urban, see V61) must not appear as an
      -- empty "0 services" card for a role that can't see any of it -- unlike the pre-import
      -- bug this catalog was built to fix, this is deliberate role-scoping, not a data gap.
      AND EXISTS (
          SELECT 1 FROM discovery.sevaone_service_master s
          JOIN discovery.service_role_visibility v ON v.service_id = s.id
          JOIN discovery.catalog_role r ON r.id = v.role_id
          WHERE s.department_id = d.id AND s.is_active AND r.role_key = p_role_key
      )
    ORDER BY d.display_order, d.department_name;
$$;
