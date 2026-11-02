-- Postgres function backing SevaOne's Dapper repositories.
-- Backs GET /api/v1/admin/gov/analytics/citizens's registration trend. Bounded to the last 90
-- days so the result set (and the JSON payload) stays small -- this is a trend chart, not an
-- export. p_district/p_taluk: NULL = no filter, see sp_get_gov_citizen_totals's doc comment.
CREATE OR REPLACE FUNCTION portal.sp_get_gov_registration_trend(p_district text DEFAULT NULL, p_taluk text DEFAULT NULL)
RETURNS TABLE (reg_date date, registrations bigint)
LANGUAGE sql STABLE AS $$
    SELECT u.created_at::date AS reg_date, count(*) AS registrations
    FROM portal.sevaone_user u
    WHERE u.created_at >= now() - interval '90 days'
      AND (p_district IS NULL OR lower(trim(u.district)) = lower(trim(p_district)))
      AND (p_taluk IS NULL OR lower(trim(u.taluk)) = lower(trim(p_taluk)))
    GROUP BY 1
    ORDER BY 1;
$$;
