-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- The landing page's other search half: services whose own name/code matches, OR whose
-- sub-department name matches (e.g. "Driving License" -> Transport -> RTO core services ->
-- the service itself; a sub-department name match pulls in every service under it). A pure
-- department-name match does NOT appear here -- that's sp_search_departments's job, so
-- searching "Revenue" doesn't flood the results with all 79 of its services as rows.
-- Capped at 50 -- a search results list, not a full department hydration.

CREATE OR REPLACE FUNCTION discovery.sp_search_services_for_role(p_query text, p_role_key character varying(64))
RETURNS TABLE (
    "Id" uuid, "DepartmentCode" character varying(64), "DepartmentName" character varying(200),
    "ServiceId" character varying(32), "SubDepartmentName" character varying(200),
    "Name" character varying(300), "NameKannada" character varying(300), "ShortDescription" text,
    "SakalaDays" integer
)
LANGUAGE sql STABLE AS $$
    SELECT
        s.id, d.department_id, d.department_name, s.service_id, sd.sub_department_name,
        s.service_name, s.service_name_kannada, s.short_description, s.sakala_days
    FROM discovery.sevaone_service_master s
    JOIN discovery.department_master d ON d.id = s.department_id
    LEFT JOIN discovery.sub_department_master sd ON sd.id = s.sub_department_id
    JOIN discovery.service_role_visibility v ON v.service_id = s.id
    JOIN discovery.catalog_role r ON r.id = v.role_id
    WHERE s.is_active = TRUE AND d.is_active = TRUE AND r.role_key = p_role_key
      AND (
        s.service_name ILIKE '%' || p_query || '%'
        OR s.service_name_kannada ILIKE '%' || p_query || '%'
        OR s.service_id ILIKE '%' || p_query || '%'
        OR sd.sub_department_name ILIKE '%' || p_query || '%'
      )
    ORDER BY s.service_name
    LIMIT 50;
$$;
