-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- The sub-department rail for one department page: real sub-departments only, each with its
-- role-visible service_count. Services with sub_department_id IS NULL ("ungrouped" -- e.g.
-- Agriculture/RDPR in the current dataset, which have no real sub-department tier at all)
-- aren't returned here; the caller computes that count as
-- sp_list_departments_for_role's total minus SUM(this function's counts) rather than a
-- separate query -- no synthetic "fallback" sub-department row needed since
-- sub_department_id was already nullable.

CREATE OR REPLACE FUNCTION discovery.sp_list_sub_departments_for_role(p_department_id uuid, p_role_key character varying(64))
RETURNS TABLE (
    "Id" uuid, "Name" character varying(200), "DisplayOrder" integer, "ServiceCount" bigint
)
LANGUAGE sql STABLE AS $$
    SELECT
        sd.id, sd.sub_department_name, sd.display_order,
        (SELECT COUNT(*) FROM discovery.sevaone_service_master s
            JOIN discovery.service_role_visibility v ON v.service_id = s.id
            JOIN discovery.catalog_role r ON r.id = v.role_id
            WHERE s.sub_department_id = sd.id AND s.is_active AND r.role_key = p_role_key)
    FROM discovery.sub_department_master sd
    WHERE sd.department_id = p_department_id AND sd.is_active = TRUE
      -- A sub-department whose services are all invisible to this role (e.g. an
      -- operator-only Sakala-token sub-department, see V61) must not appear as an empty
      -- dead-end entry for a role that can't see any of it.
      AND EXISTS (
          SELECT 1 FROM discovery.sevaone_service_master s
          JOIN discovery.service_role_visibility v ON v.service_id = s.id
          JOIN discovery.catalog_role r ON r.id = v.role_id
          WHERE s.sub_department_id = sd.id AND s.is_active AND r.role_key = p_role_key
      )
    ORDER BY sd.display_order, sd.sub_department_name;
$$;
