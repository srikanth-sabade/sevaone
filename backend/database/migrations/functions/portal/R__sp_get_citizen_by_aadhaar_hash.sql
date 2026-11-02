-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

-- Used by CitizenSessionService.EstablishAsync to detect a duplicate registration under a
-- different mobile/login for the same real Aadhaar.
CREATE OR REPLACE FUNCTION portal.sp_get_citizen_by_aadhaar_hash(p_aadhaar_hash text)
RETURNS SETOF portal.sevaone_user
LANGUAGE sql STABLE AS $$
    SELECT * FROM portal.sevaone_user WHERE aadhaar_hash = p_aadhaar_hash;
$$;
