-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION portal.sp_insert_citizen(
    p_id uuid, p_wso2_subject text, p_username text, p_name text, p_mobile text,
    p_mobile_verified_at timestamptz, p_email text, p_address text,
    p_district text, p_taluk text, p_state_name text, p_village_name text, p_date_of_birth date,
    p_gender text, p_aadhaar_masked text, p_aadhaar_hash text, p_digilocker_id text, p_photo bytea,
    p_name_kannada text,
    p_address_kannada text, p_district_kannada text, p_state_kannada text, p_village_kannada text,
    p_seva_id text, p_profile_completed boolean, p_profile_finish_pct integer,
    p_created_by_operator_id text, p_created_at timestamptz, p_updated_at timestamptz
)
RETURNS TABLE (success boolean, message text, sevaone_user_id bigint)
LANGUAGE plpgsql AS $$
DECLARE
    v_sevaone_user_id bigint;
BEGIN
    INSERT INTO portal.sevaone_user (
        id, wso2_subject, username, name, mobile, mobile_verified_at, email, address,
        district, taluk, state_name, village_name, date_of_birth, gender, aadhaar_masked,
        aadhaar_hash, digilocker_id, photo, name_kannada, address_kannada, district_kannada, state_kannada, village_kannada,
        seva_id, profile_completed, profile_finish_pct, created_by_operator_id,
        created_at, updated_at
    ) VALUES (
        p_id, p_wso2_subject, p_username, p_name, p_mobile, p_mobile_verified_at, p_email, p_address,
        p_district, p_taluk, p_state_name, p_village_name, p_date_of_birth, p_gender, p_aadhaar_masked,
        p_aadhaar_hash, p_digilocker_id, p_photo, p_name_kannada, p_address_kannada, p_district_kannada, p_state_kannada, p_village_kannada,
        p_seva_id, p_profile_completed, p_profile_finish_pct, p_created_by_operator_id,
        p_created_at, p_updated_at
    )
    RETURNING portal.sevaone_user.sevaone_user_id INTO v_sevaone_user_id;

    RETURN QUERY SELECT TRUE, 'Citizen created.'::text, v_sevaone_user_id;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('portal.sp_insert_citizen', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
