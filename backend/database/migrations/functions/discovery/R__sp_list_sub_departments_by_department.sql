-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION discovery.sp_list_sub_departments_by_department(p_department_id uuid)
RETURNS TABLE (
    "Id" uuid, "DepartmentId" uuid, "SubDepartmentName" character varying(200),
    "IsActive" boolean, "DisplayOrder" integer, "CreatedAt" timestamp with time zone
)
LANGUAGE sql STABLE AS $$
    SELECT id, department_id, sub_department_name, is_active, display_order, created_at
    FROM discovery.sub_department_master
    WHERE department_id = p_department_id AND is_active = TRUE
    ORDER BY display_order, sub_department_name;
$$;
