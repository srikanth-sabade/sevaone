-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION portal.sp_get_operator_whitelist_entry(p_user_id bigint)
RETURNS TABLE (
    user_id bigint, registration_id character varying(32), kiosk_type_id integer,
    kiosk_type character varying(20), spdi_status smallint
)
LANGUAGE sql STABLE AS $$
    SELECT user_id, registration_id, kiosk_type_id, kiosk_type, spdi_status
    FROM portal.operator_whitelist
    WHERE user_id = p_user_id;
$$;
