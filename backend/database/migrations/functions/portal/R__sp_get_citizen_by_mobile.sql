-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

-- Only matches VERIFIED mobiles (mirrors the partial unique index
-- ix_sevaone_user_mobile ... WHERE mobile_verified_at IS NOT NULL) -- an OTP-verified
-- caller should never be matched to a citizen row whose mobile was merely claimed/typed by
-- someone else and never actually confirmed.
CREATE OR REPLACE FUNCTION portal.sp_get_citizen_by_mobile(p_mobile text)
RETURNS SETOF portal.sevaone_user
LANGUAGE sql STABLE AS $$
    SELECT * FROM portal.sevaone_user WHERE mobile = p_mobile AND mobile_verified_at IS NOT NULL;
$$;
