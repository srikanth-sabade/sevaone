-- Postgres function backing SevaOne's Dapper repositories.
-- Backs GET /api/v1/admin/gov/analytics/overview's AI-vs-catalogue apply split (the same source
-- column CatalogController's citizen-facing "Frequently requested" split already uses -- see
-- discovery.service_apply_track's source column, V67__service_apply_track_source.sql).
--
-- discovery.service_apply_track has no district/taluk column of its own, only citizen_id -- a
-- District/Taluk Head scope requires joining back to portal.sevaone_user for the citizen's
-- district. This is a heavier query than the applications-table-backed functions (which filter
-- on their own column directly) -- acceptable for v1's data volume; revisit (e.g. denormalize
-- district onto service_apply_track, or add a covering index) if this proves slow in practice.
--
-- Explicit DROP: added p_department_code for the "Department" gov-admin tier (see
-- V69__gov_admin_master_claim_flow.sql), which Postgres refuses via CREATE OR REPLACE alone
-- (parameter list changed).
DROP FUNCTION IF EXISTS portal.sp_get_gov_apply_source_split(text, text);

CREATE OR REPLACE FUNCTION portal.sp_get_gov_apply_source_split(p_district text DEFAULT NULL, p_taluk text DEFAULT NULL, p_department_code text DEFAULT NULL)
RETURNS TABLE (source character varying(16), click_count bigint)
LANGUAGE sql STABLE AS $$
    SELECT t.source, count(*) AS click_count
    FROM discovery.service_apply_track t
    LEFT JOIN portal.sevaone_user u ON u.id = t.citizen_id
    WHERE (p_district IS NULL OR lower(trim(u.district)) = lower(trim(p_district)))
      AND (p_taluk IS NULL OR lower(trim(u.taluk)) = lower(trim(p_taluk)))
      AND (p_department_code IS NULL OR t.department_id = (SELECT id FROM discovery.department_master WHERE department_id = p_department_code))
    GROUP BY t.source
    ORDER BY t.source;
$$;
