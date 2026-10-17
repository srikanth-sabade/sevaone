-- Postgres function backing GET /api/v1/admin/gov/analytics/sla-trend -- the historical SLA
-- performance chart. Same on-time/late definition as sp_get_gov_sakala_sla (completed
-- applications only, gated on is_sakala_enabled AND sakala_days > 0), but bucketed by the week
-- the application was submitted instead of by department, so compliance can be tracked over
-- time. Bounded to the last 90 days, same rationale/window as sp_get_gov_lifecycle_trend and
-- sp_get_gov_registration_trend. The literal status codes 3/4 are
-- SevaOne.Domain.Enums.ApplicationStatus.Approved/Rejected, kept in sync manually as elsewhere.
DROP FUNCTION IF EXISTS portal.sp_get_gov_sla_trend(text, text, text);

CREATE OR REPLACE FUNCTION portal.sp_get_gov_sla_trend(p_district text DEFAULT NULL, p_taluk text DEFAULT NULL, p_department_code text DEFAULT NULL)
RETURNS TABLE (
    period_start date,
    total bigint,
    completed_on_time bigint,
    completed_late bigint
)
LANGUAGE sql STABLE AS $$
    WITH sakala_apps AS (
        SELECT
            a.submitted_at,
            a.sakala_due_at,
            (
                SELECT max(e.at) FROM status.application_events e
                WHERE e.application_id = a.id AND e.status = a.status
            ) AS completion_at
        FROM orchestration.applications a
        JOIN discovery.sevaone_service_master s ON s.id = a.scheme_id
        JOIN discovery.department_master d ON d.id = a.department_id
        WHERE s.is_sakala_enabled IS TRUE
          AND s.sakala_days > 0
          AND a.status IN (3, 4)
          AND a.submitted_at >= now() - interval '90 days'
          AND (p_district IS NULL OR lower(trim(a.district)) = lower(trim(p_district)))
          AND (p_taluk IS NULL OR lower(trim(a.taluk)) = lower(trim(p_taluk)))
          AND (p_department_code IS NULL OR d.department_id = p_department_code)
    )
    SELECT
        date_trunc('week', submitted_at)::date AS period_start,
        count(*) AS total,
        count(*) FILTER (WHERE completion_at <= sakala_due_at) AS completed_on_time,
        count(*) FILTER (WHERE completion_at > sakala_due_at) AS completed_late
    FROM sakala_apps
    GROUP BY 1
    ORDER BY 1;
$$;
