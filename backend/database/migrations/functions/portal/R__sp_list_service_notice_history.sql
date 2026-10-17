CREATE OR REPLACE FUNCTION portal.sp_list_service_notice_history()
RETURNS TABLE
(
    id          uuid,
    title       character varying(120),
    message     character varying(500),
    severity    character varying(16),
    was_active  boolean,
    starts_at   timestamp with time zone,
    ends_at     timestamp with time zone,
    saved_at    timestamp with time zone
)
LANGUAGE sql STABLE AS $$
    SELECT id, title, message, severity, was_active, starts_at, ends_at, saved_at
    FROM portal.service_notice_history
    ORDER BY saved_at DESC;
$$;
