-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
--
-- Gov-admin login lookup, by username (stable from creation) rather than wso2_subject (null
-- until claimed -- see V69__gov_admin_master_claim_flow.sql). GovAdminAuthService checks
-- wso2_subject IS NOT NULL itself to detect "not yet claimed".
CREATE OR REPLACE FUNCTION portal.sp_get_gov_admin_by_username(p_username text)
RETURNS TABLE (
    id uuid, username character varying(100), wso2_subject character varying(100),
    display_name character varying(200), tier character varying(20),
    assigned_district character varying(120), assigned_taluk character varying(120),
    assigned_department character varying(64), is_active boolean
)
LANGUAGE sql STABLE AS $$
    SELECT id, username, wso2_subject, display_name, tier, assigned_district, assigned_taluk, assigned_department, is_active
    FROM portal.gov_admin_master
    WHERE username = p_username;
$$;
