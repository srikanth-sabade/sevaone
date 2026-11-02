-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION discovery.sp_list_service_visibility(p_service_id uuid)
RETURNS TABLE ("RoleKey" character varying(64), "DisplayName" character varying(200), "GrantedAt" timestamptz, "GrantedBy" text)
LANGUAGE sql STABLE AS $$
    SELECT r.role_key, r.display_name, v.granted_at, v.granted_by
    FROM discovery.service_role_visibility v
    JOIN discovery.catalog_role r ON r.id = v.role_id
    WHERE v.service_id = p_service_id
    ORDER BY r.role_key;
$$;
