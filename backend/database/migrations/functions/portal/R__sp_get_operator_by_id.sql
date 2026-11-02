-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

-- Explicit DROP: return row shape changed (V34/V35 -- wallet_id replacing wallet_balance), which
-- Postgres refuses to apply via CREATE OR REPLACE alone.
DROP FUNCTION IF EXISTS portal.sp_get_operator_by_id(uuid);

CREATE OR REPLACE FUNCTION portal.sp_get_operator_by_id(p_id uuid)
RETURNS TABLE (
    id uuid, operator_code character varying(50), operator_type character varying(10),
    operator_name character varying(200), mobile_no character varying(15),
    wallet_id character varying(100),
    is_active boolean, created_at timestamp with time zone
)
LANGUAGE sql STABLE AS $$
    SELECT om.id, om.operator_id, om.operator_type, om.operator_name, om.mobile_no,
           om.wallet_id, om.is_active, om.created_at
    FROM portal.operator_master om
    WHERE om.id = p_id;
$$;
