-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

-- Explicit DROP: return row shape/param list changed (V57 -- wso2_subject replacing
-- password_hash as the claim-time credential link), which Postgres refuses to apply via
-- CREATE OR REPLACE alone.
DROP FUNCTION IF EXISTS portal.sp_claim_operator(uuid, character varying, character varying, character varying, timestamp with time zone, text, timestamp with time zone);

-- Creates the operator_master row for a self-claimed whitelist entry. password_hash is left
-- NULL -- the real credential lives in WSO2 IS (see OperatorClaimService), operator_master is
-- a link table for this claim path, not a credential store. Relies on
-- ix_operator_master_operator_code's uniqueness (not a pre-check) to catch "already claimed" --
-- avoids a check-then-insert race between two claim attempts for the same operator_id.
CREATE OR REPLACE FUNCTION portal.sp_claim_operator(
    p_id uuid, p_operator_id character varying(50), p_operator_type character varying(10),
    p_mobile_no character varying(15), p_mobile_verified_at timestamp with time zone,
    p_wso2_subject character varying(100), p_created_at timestamp with time zone
)
RETURNS TABLE (success boolean, message text, id uuid)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO portal.operator_master
        (id, operator_id, operator_type, mobile_no, mobile_verified_at, wso2_subject, is_active, created_at)
    VALUES
        (p_id, p_operator_id, p_operator_type, p_mobile_no, p_mobile_verified_at, p_wso2_subject, TRUE, p_created_at);

    RETURN QUERY SELECT TRUE, 'Operator claimed.'::text, p_id;
EXCEPTION
    WHEN unique_violation THEN
        RETURN QUERY SELECT FALSE, 'This operator ID has already been claimed.'::text, NULL::uuid;
    WHEN OTHERS THEN
        INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
        VALUES ('portal.sp_claim_operator', SQLSTATE, SQLERRM);
        RAISE;
END;
$$;
