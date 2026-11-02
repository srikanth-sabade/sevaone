-- Postgres function backing SevaOne's Dapper repositories.
-- Superadmin's activate/deactivate action. Returns TRUE only if a row was actually updated
-- (id exists) -- lets the caller distinguish "not found" from "no-op, already in that state".
CREATE OR REPLACE FUNCTION portal.sp_set_gov_admin_active(p_id uuid, p_is_active boolean)
RETURNS boolean
LANGUAGE sql AS $$
    UPDATE portal.gov_admin_master SET is_active = p_is_active WHERE id = p_id
    RETURNING true;
$$;
