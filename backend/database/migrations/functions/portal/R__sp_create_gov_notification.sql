CREATE OR REPLACE FUNCTION portal.sp_create_gov_notification(
    p_title varchar(160),
    p_body text,
    p_created_by uuid
)
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
LANGUAGE sql AS $$
    INSERT INTO portal.gov_notifications (title, body, is_active, created_by, created_at, updated_at)
    VALUES (p_title, p_body, true, p_created_by, now(), now())
    RETURNING id, title, body, is_active, created_by, created_at, updated_at;
$$;
