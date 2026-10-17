DROP FUNCTION IF EXISTS portal.sp_list_operators_for_admin(text, boolean, text);

CREATE OR REPLACE FUNCTION portal.sp_list_operators_for_admin(
    p_search text DEFAULT NULL,
    p_is_active boolean DEFAULT NULL,
    p_operator_type text DEFAULT NULL)
RETURNS TABLE (
    id uuid, operator_code character varying(50), operator_type character varying(10),
    operator_name character varying(200), mobile_no character varying(15),
    is_claimed boolean, is_active boolean, created_at timestamp with time zone)
LANGUAGE sql STABLE AS $$
    SELECT om.id, om.operator_id, om.operator_type, om.operator_name, om.mobile_no,
           (om.wso2_subject IS NOT NULL) AS is_claimed, om.is_active, om.created_at
    FROM portal.operator_master om
    WHERE (p_search IS NULL OR p_search = '' OR
           om.operator_id ILIKE '%' || p_search || '%' OR
           COALESCE(om.operator_name, '') ILIKE '%' || p_search || '%' OR
           COALESCE(om.mobile_no, '') ILIKE '%' || p_search || '%')
      AND (p_is_active IS NULL OR om.is_active = p_is_active)
      AND (p_operator_type IS NULL OR p_operator_type = '' OR om.operator_type = p_operator_type)
    ORDER BY om.created_at DESC;
$$;
