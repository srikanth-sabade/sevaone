-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION portal.sp_list_gov_admin_activity(
    p_limit integer, p_action character varying(64) DEFAULT NULL,
    p_from timestamp with time zone DEFAULT NULL, p_to timestamp with time zone DEFAULT NULL
)
RETURNS TABLE (
    "Id" uuid, "ActorUsername" character varying(100), "ActorDisplayName" character varying(200),
    "Action" character varying(64), "Target" text, "Detail" text, "At" timestamp with time zone)
LANGUAGE sql STABLE AS $$
    SELECT id, actor_username, actor_display_name, action, target, detail, at
    FROM portal.gov_admin_activity_log
    WHERE (p_action IS NULL OR action = p_action)
      AND (p_from IS NULL OR at >= p_from)
      AND (p_to IS NULL OR at <= p_to)
    ORDER BY at DESC
    LIMIT greatest(coalesce(p_limit, 100), 1);
$$;
