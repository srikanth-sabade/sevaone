DROP FUNCTION IF EXISTS portal.sp_get_gov_service_usage(text, text, text, timestamptz, timestamptz);

CREATE OR REPLACE FUNCTION portal.sp_get_gov_service_usage(
    p_district text DEFAULT NULL,
    p_taluk text DEFAULT NULL,
    p_department_code text DEFAULT NULL,
    p_since timestamptz DEFAULT NULL,
    p_until timestamptz DEFAULT NULL)
RETURNS TABLE (
    service_code character varying(32), service_name character varying(300),
    department_code character varying(64), department_name character varying(200),
    total_apply_clicks bigint, ai_apply_clicks bigint, catalogue_apply_clicks bigint,
    last_used_at timestamp with time zone)
LANGUAGE sql STABLE AS $$
    SELECT s.service_id, s.service_name, d.department_id, d.department_name,
           count(t.id) AS total_apply_clicks,
           count(t.id) FILTER (WHERE t.source = 'ai') AS ai_apply_clicks,
           count(t.id) FILTER (WHERE t.source = 'catalogue') AS catalogue_apply_clicks,
           max(t.created_at) AS last_used_at
    FROM discovery.sevaone_service_master s
    JOIN discovery.department_master d ON d.id = s.department_id
    LEFT JOIN discovery.service_apply_track t
      ON t.scheme_id = s.id
     AND (p_since IS NULL OR t.created_at >= p_since)
     AND (p_until IS NULL OR t.created_at < p_until)
     AND (p_district IS NULL OR EXISTS (
            SELECT 1 FROM portal.sevaone_user u
            WHERE u.id = t.citizen_id
              AND lower(trim(u.district)) = lower(trim(p_district))))
     AND (p_taluk IS NULL OR EXISTS (
            SELECT 1 FROM portal.sevaone_user u
            WHERE u.id = t.citizen_id
              AND lower(trim(u.taluk)) = lower(trim(p_taluk))))
    WHERE s.is_active IS TRUE
      AND d.is_active IS TRUE
      AND (p_department_code IS NULL OR d.department_id = p_department_code)
    GROUP BY s.id, d.id, s.service_id, s.service_name, d.department_id, d.department_name
    ORDER BY total_apply_clicks DESC, s.service_name;
$$;
