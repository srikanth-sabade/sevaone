-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION portal.sp_update_citizen(
    p_id uuid, p_name text, p_mobile text, p_mobile_verified_at timestamptz,
    p_email text, p_address text, p_district text, p_taluk text, p_state_name text, p_village_name text,
    p_date_of_birth date, p_gender text, p_aadhaar_masked text, p_aadhaar_hash text,
    p_digilocker_id text, p_photo bytea,
    p_name_kannada text, p_address_kannada text, p_district_kannada text, p_state_kannada text,
    p_village_kannada text, p_profile_completed boolean, p_profile_finish_pct integer,
    p_updated_at timestamptz
)
RETURNS TABLE (success boolean, message text)
LANGUAGE plpgsql AS $$
DECLARE
    v_rows integer;
BEGIN
    UPDATE portal.sevaone_user SET
        name = p_name, mobile = p_mobile, mobile_verified_at = p_mobile_verified_at,
        email = p_email,
        address = p_address, district = p_district, taluk = p_taluk, state_name = p_state_name,
        village_name = p_village_name, date_of_birth = p_date_of_birth, gender = p_gender,
        aadhaar_masked = p_aadhaar_masked, aadhaar_hash = p_aadhaar_hash,
        digilocker_id = p_digilocker_id, photo = p_photo,
        name_kannada = p_name_kannada, address_kannada = p_address_kannada,
        district_kannada = p_district_kannada, state_kannada = p_state_kannada,
        village_kannada = p_village_kannada,
        profile_completed = p_profile_completed, profile_finish_pct = p_profile_finish_pct,
        updated_at = p_updated_at
    WHERE id = p_id;

    GET DIAGNOSTICS v_rows = ROW_COUNT;
    RETURN QUERY SELECT (v_rows = 1), (CASE WHEN v_rows = 1 THEN 'Citizen updated.' ELSE 'Citizen not found.' END)::text;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('portal.sp_update_citizen', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
