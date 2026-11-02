-- Postgres function backing SevaOne's Dapper repositories.
-- Backs GET /api/v1/admin/gov/analytics/lifecycle's trend chart. Bounded to the last 90 days,
-- same rationale as sp_get_gov_registration_trend. The literal status codes 3/4 are
-- SevaOne.Domain.Enums.ApplicationStatus.Approved/Rejected -- kept in sync manually since this
-- is a plain SQL function, not app code; if ApplicationStatus's numbering ever changes, update
-- here too.
--
-- Explicit DROP: added p_department_code for the "Department" gov-admin tier -- see
-- V69__gov_admin_master_claim_flow.sql.
DROP FUNCTION IF EXISTS portal.sp_get_gov_lifecycle_trend(text, text);

CREATE OR REPLACE FUNCTION portal.sp_get_gov_lifecycle_trend(p_district text DEFAULT NULL, p_taluk text DEFAULT NULL, p_department_code text DEFAULT NULL)
RETURNS TABLE (app_date date, submitted bigint, approved bigint, rejected bigint)
LANGUAGE sql STABLE AS $$
    SELECT
        a.submitted_at::date AS app_date,
        count(*) AS submitted,
        count(*) FILTER (WHERE a.status = 3) AS approved,
        count(*) FILTER (WHERE a.status = 4) AS rejected
    FROM orchestration.applications a
    WHERE a.submitted_at >= now() - interval '90 days'
      AND (p_district IS NULL OR lower(trim(a.district)) = lower(trim(p_district)))
      AND (p_taluk IS NULL OR lower(trim(a.taluk)) = lower(trim(p_taluk)))
      AND (p_department_code IS NULL OR a.department_id = (SELECT id FROM discovery.department_master WHERE department_id = p_department_code))
    GROUP BY 1
    ORDER BY 1;
$$;
