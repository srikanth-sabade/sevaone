CREATE OR REPLACE FUNCTION portal.sp_delete_service_notice(p_deleted_by uuid)
RETURNS void
LANGUAGE sql AS $$
    WITH deleted AS (
        UPDATE portal.service_notice
        SET is_active = false, updated_by = p_deleted_by, updated_at = now()
        WHERE id = 1
          AND is_active
        RETURNING title, message, severity, true AS was_active, starts_at, ends_at
    )
    INSERT INTO portal.service_notice_history
        (id, title, message, severity, was_active, starts_at, ends_at, saved_by, saved_at)
    SELECT gen_random_uuid(), title, message, severity, was_active, starts_at, ends_at, p_deleted_by, now()
    FROM deleted;
$$;
