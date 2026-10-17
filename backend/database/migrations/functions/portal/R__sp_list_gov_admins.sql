-- Postgres function backing SevaOne's Dapper repositories.
-- Backs the Superadmin/ChiefMinister admin-management list. `claimed` is derived from
-- wso2_subject's presence -- see V69__gov_admin_master_claim_flow.sql.
CREATE OR REPLACE FUNCTION portal.sp_list_gov_admins()
RETURNS TABLE (
    id uuid, username character varying(100), display_name character varying(200),
    tier character varying(20), assigned_district character varying(120),
    assigned_taluk character varying(120), assigned_department character varying(64),
    is_active boolean, claimed boolean, created_at timestamp with time zone
)
LANGUAGE sql STABLE AS $$
    SELECT id, username, display_name, tier, assigned_district, assigned_taluk, assigned_department,
           is_active, (wso2_subject IS NOT NULL) AS claimed, created_at
    FROM portal.gov_admin_master
    ORDER BY created_at DESC;
$$;
