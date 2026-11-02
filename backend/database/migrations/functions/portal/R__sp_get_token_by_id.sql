-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION portal.sp_get_token_by_id(p_id uuid)
RETURNS SETOF portal.sevaone_token
LANGUAGE sql STABLE AS $$
    SELECT * FROM portal.sevaone_token WHERE id = p_id;
$$;
