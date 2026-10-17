CREATE OR REPLACE FUNCTION portal.sp_get_service_notice(p_active_only boolean DEFAULT false)
RETURNS TABLE
(
    title character varying(120),
    message character varying(500),
    severity character varying(16),
    is_active boolean,
    starts_at timestamp with time zone,
    ends_at timestamp with time zone,
    updated_at timestamp with time zone
)
LANGUAGE sql STABLE AS $$
    SELECT title, message, severity, is_active, starts_at, ends_at, updated_at
    FROM portal.service_notice
    WHERE id = 1
      AND (
          NOT p_active_only
          OR (
              is_active
              AND (starts_at IS NULL OR starts_at <= now())
              AND (ends_at IS NULL OR ends_at > now())
          )
      );
$$;
