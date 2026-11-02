CREATE OR REPLACE FUNCTION portal.sp_upsert_service_notice(
    p_title character varying(120),
    p_message character varying(500),
    p_severity character varying(16),
    p_is_active boolean,
    p_starts_at timestamp with time zone,
    p_ends_at timestamp with time zone,
    p_updated_by uuid
)
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
LANGUAGE sql AS $$
    WITH archived AS (
        INSERT INTO portal.service_notice_history
            (id, title, message, severity, was_active, starts_at, ends_at, saved_by, saved_at)
        SELECT gen_random_uuid(), title, message, severity, is_active, starts_at, ends_at, updated_by, updated_at
        FROM portal.service_notice
        WHERE id = 1
    ), saved AS (
        INSERT INTO portal.service_notice
        (id, title, message, severity, is_active, starts_at, ends_at, updated_by, updated_at)
        VALUES
        (1, p_title, p_message, p_severity, p_is_active, p_starts_at, p_ends_at, p_updated_by, now())
        ON CONFLICT (id) DO UPDATE SET
        title = EXCLUDED.title,
        message = EXCLUDED.message,
        severity = EXCLUDED.severity,
        is_active = EXCLUDED.is_active,
        starts_at = EXCLUDED.starts_at,
        ends_at = EXCLUDED.ends_at,
        updated_by = EXCLUDED.updated_by,
        updated_at = now()
        RETURNING title, message, severity, is_active, starts_at, ends_at, updated_at
    )
    SELECT title, message, severity, is_active, starts_at, ends_at, updated_at
    FROM saved;
$$;
