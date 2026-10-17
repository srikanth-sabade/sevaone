-- Postgres function backing SevaOne's Dapper repositories.
-- Backs GET /api/v1/admin/gov/analytics/departments's headline counts. Deliberately no
-- p_district/p_taluk params -- department/service counts aren't meaningfully district-scoped,
-- so this endpoint is statewide-only (Superadmin/Chief Minister/Govt Official); the controller
-- rejects District/Taluk Head before this is ever called (see GovAdminController).
CREATE OR REPLACE FUNCTION portal.sp_get_gov_department_service_counts()
RETURNS TABLE (department_count bigint, active_service_count bigint)
LANGUAGE sql STABLE AS $$
    SELECT
        (SELECT count(*) FROM discovery.department_master WHERE is_active) AS department_count,
        (SELECT count(*) FROM discovery.sevaone_service_master WHERE is_active) AS active_service_count;
$$;
