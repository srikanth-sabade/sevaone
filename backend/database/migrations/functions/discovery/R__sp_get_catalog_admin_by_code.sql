-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION discovery.sp_get_catalog_admin_by_code(p_admin_id character varying(50))
RETURNS TABLE ("Id" uuid, "AdminId" character varying(50), "AdminName" character varying(200), "PasswordHash" text, "IsActive" boolean)
LANGUAGE sql STABLE AS $$
    SELECT id, admin_id, admin_name, password_hash, is_active
    FROM discovery.catalog_admin_master
    WHERE admin_id = p_admin_id;
$$;
