-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

-- Explicit DROP: return row shape changed (V57 -- added wso2_subject, needed to decide whether
-- OperatorAuthService.LoginAsync verifies against WSO2 IS or the legacy local password_hash),
-- which Postgres refuses to apply via CREATE OR REPLACE alone.
DROP FUNCTION IF EXISTS portal.sp_get_operator_credentials(text);

-- Deliberately separate from portal.sp_get_operator_by_code (see that function): password_hash
-- must never come back through the general-purpose OperatorMaster read used by dept-facing
-- code paths -- only the operator-login path itself needs it.
CREATE OR REPLACE FUNCTION portal.sp_get_operator_credentials(p_operator_code text)
RETURNS TABLE (
    id uuid, operator_code character varying(50), operator_name character varying(200),
    mobile_no character varying(15), password_hash text, wso2_subject character varying(100),
    is_active boolean
)
LANGUAGE sql STABLE AS $$
    SELECT id, operator_id, operator_name, mobile_no, password_hash, wso2_subject, is_active
    FROM portal.operator_master
    WHERE operator_id = p_operator_code;
$$;
