-- Postgres function backing SevaOne's Dapper repositories.
-- Backs GET /api/v1/admin/gov/analytics/departments's per-department service counts. Statewide
-- only -- see sp_get_gov_department_service_counts's doc comment.
CREATE OR REPLACE FUNCTION portal.sp_get_gov_department_breakdown()
RETURNS TABLE (department_code character varying(64), department_name character varying(200), service_count bigint)
LANGUAGE sql STABLE AS $$
    SELECT d.department_id, d.department_name, count(s.id) AS service_count
    FROM discovery.department_master d
    LEFT JOIN discovery.sevaone_service_master s ON s.department_id = d.id AND s.is_active
    WHERE d.is_active
    GROUP BY d.department_id, d.department_name
    ORDER BY d.department_name;
$$;
