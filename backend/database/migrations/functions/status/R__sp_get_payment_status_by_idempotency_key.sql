-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION status.sp_get_payment_status_by_idempotency_key(p_idempotency_key character varying(100))
RETURNS SETOF status.payment_status_post
LANGUAGE sql STABLE AS $$
    SELECT * FROM status.payment_status_post WHERE idempotency_key = p_idempotency_key;
$$;
