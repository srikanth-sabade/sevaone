CREATE OR REPLACE FUNCTION portal.sp_set_gov_notification_active(
    p_id uuid,
    p_is_active boolean,
    p_updated_by uuid
)
RETURNS void
LANGUAGE sql AS $$
    UPDATE portal.gov_notifications
    SET is_active = p_is_active, updated_at = now()
    WHERE id = p_id;
$$;
