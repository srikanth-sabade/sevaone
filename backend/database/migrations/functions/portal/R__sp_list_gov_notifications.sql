CREATE OR REPLACE FUNCTION portal.sp_list_gov_notifications(p_active_only boolean DEFAULT false)
RETURNS TABLE
(
    id          uuid,
    title       varchar(160),
    body        text,
    is_active   boolean,
    created_by  uuid,
    created_at  timestamptz,
    updated_at  timestamptz
)
LANGUAGE sql STABLE AS $$
    SELECT id, title, body, is_active, created_by, created_at, updated_at
    FROM portal.gov_notifications
    WHERE NOT p_active_only OR is_active
    ORDER BY is_active DESC, created_at DESC;
$$;
