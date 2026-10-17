-- Postgres function backing SevaOne's Dapper repositories.
-- Backs GET /api/v1/admin/gov/analytics/discovery's headline totals. Same district/taluk-via-
-- citizen-join caveat as sp_get_gov_apply_source_split -- see that function's doc comment.
--
-- Explicit DROP: added p_department_code for the "Department" gov-admin tier -- see
-- V69__gov_admin_master_claim_flow.sql.
DROP FUNCTION IF EXISTS portal.sp_get_gov_discovery_totals(text, text);

CREATE OR REPLACE FUNCTION portal.sp_get_gov_discovery_totals(p_district text DEFAULT NULL, p_taluk text DEFAULT NULL, p_department_code text DEFAULT NULL)
RETURNS TABLE (total_apply_clicks bigint, ai_apply_clicks bigint, catalogue_apply_clicks bigint)
LANGUAGE sql STABLE AS $$
    SELECT
        count(*) AS total_apply_clicks,
        count(*) FILTER (WHERE t.source = 'ai') AS ai_apply_clicks,
        count(*) FILTER (WHERE t.source = 'catalogue') AS catalogue_apply_clicks
    FROM discovery.service_apply_track t
    LEFT JOIN portal.sevaone_user u ON u.id = t.citizen_id
    WHERE (p_district IS NULL OR lower(trim(u.district)) = lower(trim(p_district)))
      AND (p_taluk IS NULL OR lower(trim(u.taluk)) = lower(trim(p_taluk)))
      AND (p_department_code IS NULL OR t.department_id = (SELECT id FROM discovery.department_master WHERE department_id = p_department_code));
$$;
