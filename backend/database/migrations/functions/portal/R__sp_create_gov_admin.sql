-- Postgres function backing SevaOne's Dapper repositories.
-- Superadmin's "create admin" action: inserts a not-yet-claimed gov_admin_master row
-- (wso2_subject/mobile stay NULL until GovAdminClaimService's first-time-login claim -- see
-- V69__gov_admin_master_claim_flow.sql). Relies on ix_gov_admin_master_username's uniqueness
-- (not a pre-check) to catch a duplicate username, same race-safety pattern as
-- sp_claim_operator.
CREATE OR REPLACE FUNCTION portal.sp_create_gov_admin(
    p_id uuid, p_username character varying(100), p_display_name character varying(200),
    p_tier character varying(20), p_assigned_district character varying(120),
    p_assigned_taluk character varying(120), p_assigned_department character varying(64),
    p_created_at timestamp with time zone
)
RETURNS TABLE (success boolean, message text, id uuid)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO portal.gov_admin_master
        (id, username, wso2_subject, display_name, tier, assigned_district, assigned_taluk, assigned_department, is_active, created_at)
    VALUES
        (p_id, p_username, NULL, p_display_name, p_tier, p_assigned_district, p_assigned_taluk, p_assigned_department, TRUE, p_created_at);

    RETURN QUERY SELECT TRUE, 'Admin created.'::text, p_id;
EXCEPTION
    WHEN unique_violation THEN
        RETURN QUERY SELECT FALSE, 'This username is already in use.'::text, NULL::uuid;
    WHEN OTHERS THEN
        INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
        VALUES ('portal.sp_create_gov_admin', SQLSTATE, SQLERRM);
        RAISE;
END;
$$;
