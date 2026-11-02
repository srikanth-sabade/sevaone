-- Postgres function backing GET /api/v1/admin/gov/analytics/application-aging. Extends the same
-- machinery as sp_get_gov_sakala_sla: applications has no completion timestamp of its own, so
-- completion time for status IN (3,4) still comes from status.application_events (MAX(at) WHERE
-- status = a.status). The literal status codes 3/4 are
-- SevaOne.Domain.Enums.ApplicationStatus.Approved/Rejected, same manually-kept-in-sync
-- convention as every other portal.sp_get_gov_* function that reads application status.
--
-- Age buckets are half-open day intervals measured from submitted_at for currently-open
-- (non-terminal) applications: [0,4) / [4,8) / [8,15) / [15,31) / [31,inf) days, labeled
-- 0-3 / 4-7 / 8-14 / 15-30 / 30-plus on the frontend -- the last bucket's real boundary is 31+
-- days, not 30, kept this way so the five buckets are contiguous and non-overlapping.
--
-- due_within_24h and avg_processing_hours are deliberately gated differently: due_within_24h
-- only counts genuinely Sakala-governed applications (is_sakala_enabled AND sakala_days > 0,
-- same gate as sp_get_gov_sakala_sla) since sakala_due_at is computed for every application but
-- is only a legally meaningful deadline for Sakala-enabled services. avg_processing_hours is
-- NOT gated on Sakala -- processing time is meaningful for every application regardless of
-- whether it carries a Sakala deadline.
DROP FUNCTION IF EXISTS portal.sp_get_gov_application_aging(text, text, text);

CREATE OR REPLACE FUNCTION portal.sp_get_gov_application_aging(p_district text DEFAULT NULL, p_taluk text DEFAULT NULL, p_department_code text DEFAULT NULL)
RETURNS TABLE (
    department_code character varying(64),
    department_name character varying(200),
    open_applications bigint,
    due_within_24h bigint,
    age_0_3_days bigint,
    age_4_7_days bigint,
    age_8_14_days bigint,
    age_15_30_days bigint,
    age_30_plus_days bigint,
    avg_processing_hours double precision
)
LANGUAGE sql STABLE AS $$
    WITH scoped_apps AS (
        SELECT
            a.status,
            a.submitted_at,
            a.sakala_due_at,
            d.department_id AS department_code,
            d.department_name AS department_name,
            s.is_sakala_enabled,
            s.sakala_days,
            (
                SELECT max(e.at) FROM status.application_events e
                WHERE e.application_id = a.id AND e.status = a.status
            ) AS completion_at
        FROM orchestration.applications a
        JOIN discovery.sevaone_service_master s ON s.id = a.scheme_id
        JOIN discovery.department_master d ON d.id = a.department_id
        WHERE (p_district IS NULL OR lower(trim(a.district)) = lower(trim(p_district)))
          AND (p_taluk IS NULL OR lower(trim(a.taluk)) = lower(trim(p_taluk)))
          AND (p_department_code IS NULL OR d.department_id = p_department_code)
    )
    SELECT
        department_code,
        department_name,
        count(*) FILTER (WHERE status NOT IN (3, 4)) AS open_applications,
        count(*) FILTER (
            WHERE status NOT IN (3, 4)
              AND is_sakala_enabled IS TRUE AND sakala_days > 0
              AND sakala_due_at BETWEEN now() AND now() + interval '24 hours'
        ) AS due_within_24h,
        count(*) FILTER (WHERE status NOT IN (3, 4) AND now() - submitted_at < interval '4 days') AS age_0_3_days,
        count(*) FILTER (WHERE status NOT IN (3, 4) AND now() - submitted_at >= interval '4 days' AND now() - submitted_at < interval '8 days') AS age_4_7_days,
        count(*) FILTER (WHERE status NOT IN (3, 4) AND now() - submitted_at >= interval '8 days' AND now() - submitted_at < interval '15 days') AS age_8_14_days,
        count(*) FILTER (WHERE status NOT IN (3, 4) AND now() - submitted_at >= interval '15 days' AND now() - submitted_at < interval '31 days') AS age_15_30_days,
        count(*) FILTER (WHERE status NOT IN (3, 4) AND now() - submitted_at >= interval '31 days') AS age_30_plus_days,
        avg(EXTRACT(EPOCH FROM (completion_at - submitted_at)) / 3600.0) FILTER (WHERE status IN (3, 4)) AS avg_processing_hours
    FROM scoped_apps
    GROUP BY department_code, department_name
    ORDER BY department_name;
$$;
