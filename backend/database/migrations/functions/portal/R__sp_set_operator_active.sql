CREATE OR REPLACE FUNCTION portal.sp_set_operator_active(p_id uuid, p_is_active boolean)
RETURNS boolean
LANGUAGE sql AS $$
    UPDATE portal.operator_master SET is_active = p_is_active WHERE id = p_id
    RETURNING true;
$$;
