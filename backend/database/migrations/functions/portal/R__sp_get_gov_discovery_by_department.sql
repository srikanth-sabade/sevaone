-- Postgres function backing SevaOne's Dapper repositories.
-- Backs GET /api/v1/admin/gov/analytics/discovery's per-department AI-vs-catalogue breakdown.
-- Same district/taluk-via-citizen-join caveat as sp_get_gov_apply_source_split.
--
-- Explicit DROP: added p_department_code for the "Department" gov-admin tier -- see
-- V69__gov_admin_master_claim_flow.sql. Filters directly on d.department_id since this query
-- already joins department_master.
DROP FUNCTION IF EXISTS portal.sp_get_gov_discovery_by_department(text, text);

CREATE OR REPLACE FUNCTION portal.sp_get_gov_discovery_by_department(p_district text DEFAULT NULL, p_taluk text DEFAULT NULL, p_department_code text DEFAULT NULL)
RETURNS TABLE (department_code character varying(64), ai_count bigint, catalogue_count bigint)
LANGUAGE sql STABLE AS $$
    SELECT
        d.department_id AS department_code,
        count(*) FILTER (WHERE t.source = 'ai') AS ai_count,
        count(*) FILTER (WHERE t.source = 'catalogue') AS catalogue_count
    FROM discovery.service_apply_track t
    JOIN discovery.department_master d ON d.id = t.department_id
    LEFT JOIN portal.sevaone_user u ON u.id = t.citizen_id
    WHERE (p_district IS NULL OR lower(trim(u.district)) = lower(trim(p_district)))
      AND (p_taluk IS NULL OR lower(trim(u.taluk)) = lower(trim(p_taluk)))
      AND (p_department_code IS NULL OR d.department_id = p_department_code)
    GROUP BY d.department_id
    ORDER BY d.department_id;
$$;
