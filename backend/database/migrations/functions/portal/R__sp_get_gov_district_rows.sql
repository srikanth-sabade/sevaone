-- Postgres function backing SevaOne's Dapper repositories.
-- Backs GET /api/v1/admin/gov/analytics/districts. orchestration.applications carries its own
-- district/taluk columns directly (a better scoping join target than going through the citizen
-- table); portal.sevaone_user's district/taluk give the citizen-count side. Both are grouped
-- independently then combined with a FULL OUTER JOIN so a district with citizens but zero
-- applications (or vice versa) still shows up with a 0, not a missing row.
-- p_district/p_taluk: NULL = no filter, see sp_get_gov_citizen_totals's doc comment.
--
-- Explicit DROP: return row shape changed (added the per-district status breakdown for the
-- admin-panel Karnataka map's metric toggle), which Postgres refuses via CREATE OR REPLACE
-- alone -- see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
--
-- submitted/in_progress/completed/rejected use the raw ApplicationStatus stages (0 / 1-2 / 3 / 4
-- respectively) rather than the 3-bucket Completed/Pending/Rejected naming used elsewhere
-- (GovDashboardService.BucketStatuses) -- the district map deliberately shows "just submitted"
-- as its own category, distinct from "in progress after submission", per the admin-panel's
-- district-view requirement.
DROP FUNCTION IF EXISTS portal.sp_get_gov_district_rows(text, text);

CREATE OR REPLACE FUNCTION portal.sp_get_gov_district_rows(p_district text DEFAULT NULL, p_taluk text DEFAULT NULL)
RETURNS TABLE (
    district text, taluk text, application_count bigint, citizen_count bigint,
    submitted_count bigint, in_progress_count bigint, completed_count bigint, rejected_count bigint
)
LANGUAGE sql STABLE AS $$
    WITH app_counts AS (
        SELECT lower(trim(a.district)) AS district_key, lower(trim(a.taluk)) AS taluk_key,
               trim(a.district) AS district, trim(a.taluk) AS taluk,
               count(*) AS application_count,
               count(*) FILTER (WHERE a.status = 0) AS submitted_count,
               count(*) FILTER (WHERE a.status IN (1, 2)) AS in_progress_count,
               count(*) FILTER (WHERE a.status = 3) AS completed_count,
               count(*) FILTER (WHERE a.status = 4) AS rejected_count
        FROM orchestration.applications a
        WHERE a.district IS NOT NULL
          AND (p_district IS NULL OR lower(trim(a.district)) = lower(trim(p_district)))
          AND (p_taluk IS NULL OR lower(trim(a.taluk)) = lower(trim(p_taluk)))
        GROUP BY 1, 2, 3, 4
    ),
    citizen_counts AS (
        SELECT lower(trim(u.district)) AS district_key, lower(trim(u.taluk)) AS taluk_key,
               trim(u.district) AS district, trim(u.taluk) AS taluk, count(*) AS citizen_count
        FROM portal.sevaone_user u
        WHERE u.district IS NOT NULL
          AND (p_district IS NULL OR lower(trim(u.district)) = lower(trim(p_district)))
          AND (p_taluk IS NULL OR lower(trim(u.taluk)) = lower(trim(p_taluk)))
        GROUP BY 1, 2, 3, 4
    )
    SELECT
        coalesce(a.district, c.district) AS district,
        coalesce(a.taluk, c.taluk) AS taluk,
        coalesce(a.application_count, 0) AS application_count,
        coalesce(c.citizen_count, 0) AS citizen_count,
        coalesce(a.submitted_count, 0) AS submitted_count,
        coalesce(a.in_progress_count, 0) AS in_progress_count,
        coalesce(a.completed_count, 0) AS completed_count,
        coalesce(a.rejected_count, 0) AS rejected_count
    FROM app_counts a
    FULL OUTER JOIN citizen_counts c
        ON a.district_key = c.district_key AND a.taluk_key IS NOT DISTINCT FROM c.taluk_key
    ORDER BY 1, 2;
$$;
