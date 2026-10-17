-- Postgres function backing SevaOne's Dapper repositories.
-- Backs GET /api/v1/admin/gov/analytics/overview and .../lifecycle. Returns the raw integer
-- status (orchestration.applications.status, backed by SevaOne.Domain.Enums.ApplicationStatus)
-- rather than a name -- GovDashboardService maps it to the enum's string name, keeping the
-- mapping in one C# place instead of duplicating it in SQL.
--
-- Explicit DROP: added p_department_code for the "Department" gov-admin tier -- see
-- V69__gov_admin_master_claim_flow.sql.
DROP FUNCTION IF EXISTS portal.sp_get_gov_application_status_breakdown(text, text);

CREATE OR REPLACE FUNCTION portal.sp_get_gov_application_status_breakdown(p_district text DEFAULT NULL, p_taluk text DEFAULT NULL, p_department_code text DEFAULT NULL)
RETURNS TABLE (status integer, status_count bigint)
LANGUAGE sql STABLE AS $$
    SELECT a.status, count(*) AS status_count
    FROM orchestration.applications a
    WHERE (p_district IS NULL OR lower(trim(a.district)) = lower(trim(p_district)))
      AND (p_taluk IS NULL OR lower(trim(a.taluk)) = lower(trim(p_taluk)))
      AND (p_department_code IS NULL OR a.department_id = (SELECT id FROM discovery.department_master WHERE department_id = p_department_code))
    GROUP BY a.status
    ORDER BY a.status;
$$;
