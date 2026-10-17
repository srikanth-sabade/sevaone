-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION discovery.sp_list_catalog_roles()
RETURNS TABLE ("Id" uuid, "RoleKey" character varying(64), "DisplayName" character varying(200), "Description" text)
LANGUAGE sql STABLE AS $$
    SELECT id, role_key, display_name, description FROM discovery.catalog_role ORDER BY role_key;
$$;
