-- Postgres function backing GET /api/v1/admin/gov/analytics/sakala-sla. Sakala is Karnataka's
-- legally-mandated service-delivery-timeline scheme -- sevaone_service_master.sakala_days/
-- is_sakala_enabled mark which services carry a deadline, and orchestration.applications.
-- sakala_due_at is already computed at submit time (submitted_at + service.sakala_days), so no
-- new due-date math is needed here. Only sakala_days > 0 AND is_sakala_enabled IS TRUE counts --
-- sakala_days can carry a stale figure on a service that was never actually enabled for Sakala
-- (per that column's own doc comment in V42__sevaone_service_master_sakala_kannada_version.sql).
--
-- applications has no completion timestamp of its own -- the actual status-change time comes
-- from status.application_events, taking the latest event row matching the application's own
-- current terminal status. The literal status codes 3/4 are
-- SevaOne.Domain.Enums.ApplicationStatus.Approved/Rejected, same manually-kept-in-sync
-- convention as sp_get_gov_lifecycle_trend.
--
-- Grouped by department (not district) -- SLA compliance is fundamentally a per-department
-- delivery metric; district/taluk/department params only narrow which applications are counted,
-- same nullable-filter convention as every other portal.sp_get_gov_* function.
DROP FUNCTION IF EXISTS portal.sp_get_gov_sakala_sla(text, text, text);

CREATE OR REPLACE FUNCTION portal.sp_get_gov_sakala_sla(p_district text DEFAULT NULL, p_taluk text DEFAULT NULL, p_department_code text DEFAULT NULL)
RETURNS TABLE (
    department_code character varying(64),
    department_name character varying(200),
    total_sakala_applications bigint,
    completed_on_time bigint,
    completed_late bigint,
    pending_within_sla bigint,
    pending_overdue bigint
)
LANGUAGE sql STABLE AS $$
    WITH sakala_apps AS (
        SELECT
            a.status,
            a.sakala_due_at,
            d.department_id AS department_code,
            d.department_name AS department_name,
            (
                SELECT max(e.at) FROM status.application_events e
                WHERE e.application_id = a.id AND e.status = a.status
            ) AS completion_at
        FROM orchestration.applications a
        JOIN discovery.sevaone_service_master s ON s.id = a.scheme_id
        JOIN discovery.department_master d ON d.id = a.department_id
        WHERE s.is_sakala_enabled IS TRUE
          AND s.sakala_days > 0
          AND (p_district IS NULL OR lower(trim(a.district)) = lower(trim(p_district)))
          AND (p_taluk IS NULL OR lower(trim(a.taluk)) = lower(trim(p_taluk)))
          AND (p_department_code IS NULL OR d.department_id = p_department_code)
    )
    SELECT
        department_code,
        department_name,
        count(*) AS total_sakala_applications,
        count(*) FILTER (WHERE status IN (3, 4) AND completion_at <= sakala_due_at) AS completed_on_time,
        count(*) FILTER (WHERE status IN (3, 4) AND completion_at > sakala_due_at) AS completed_late,
        count(*) FILTER (WHERE status NOT IN (3, 4) AND now() <= sakala_due_at) AS pending_within_sla,
        count(*) FILTER (WHERE status NOT IN (3, 4) AND now() > sakala_due_at) AS pending_overdue
    FROM sakala_apps
    GROUP BY department_code, department_name
    ORDER BY department_name;
$$;
