-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- The Service Catalogue landing page's search: matching a department by name surfaces just
-- the department card, not every one of its services as rows (see sp_search_services_for_role
-- for that half) -- searching "Revenue" should return one Revenue Department card, not 79 rows.
-- Departments aren't role-gated themselves, but ServiceCount is -- p_role_key keeps this
-- consistent with sp_list_departments_for_role's counts rather than showing a raw total.

CREATE OR REPLACE FUNCTION discovery.sp_search_departments(p_query text, p_role_key character varying(64))
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
    WHERE d.is_active = TRUE AND d.department_name ILIKE '%' || p_query || '%'
    ORDER BY d.department_name;
$$;
