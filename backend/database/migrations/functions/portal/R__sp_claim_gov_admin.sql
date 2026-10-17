-- Postgres function backing SevaOne's Dapper repositories.
-- Completes a gov-admin's first-time-login claim (GovAdminClaimService): sets
-- wso2_subject/mobile_no/mobile_verified_at on an existing row, found by username. UPDATE ...
-- WHERE wso2_subject IS NULL is the actual race-safe "not already claimed" guard (not a
-- pre-check-then-act) -- mirrors sp_claim_operator's unique_violation approach, just shaped as a
-- conditional UPDATE since this claim UPDATEs a pre-existing row instead of INSERTing a new one.
CREATE OR REPLACE FUNCTION portal.sp_claim_gov_admin(
    p_username character varying(100), p_wso2_subject character varying(100),
    p_mobile_no character varying(15), p_mobile_verified_at timestamp with time zone
)
RETURNS TABLE (success boolean, message text)
LANGUAGE plpgsql AS $$
DECLARE
    v_updated integer;
    v_exists boolean;
BEGIN
    UPDATE portal.gov_admin_master
    SET wso2_subject = p_wso2_subject, mobile_no = p_mobile_no, mobile_verified_at = p_mobile_verified_at
    WHERE username = p_username AND wso2_subject IS NULL AND is_active = TRUE;

    GET DIAGNOSTICS v_updated = ROW_COUNT;
    IF v_updated > 0 THEN
        RETURN QUERY SELECT TRUE, 'Claimed.'::text;
        RETURN;
    END IF;

    SELECT EXISTS(SELECT 1 FROM portal.gov_admin_master WHERE username = p_username) INTO v_exists;
    IF NOT v_exists THEN
        RETURN QUERY SELECT FALSE, 'This username is not recognized.'::text;
    ELSE
        RETURN QUERY SELECT FALSE, 'This account has already been claimed, or is deactivated.'::text;
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
        VALUES ('portal.sp_claim_gov_admin', SQLSTATE, SQLERRM);
        RAISE;
END;
$$;
