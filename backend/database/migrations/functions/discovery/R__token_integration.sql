-- Postgres functions backing the token-integration admin screen (see V75__token_integration.sql).
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied whenever this file changes.
-- Operator-only Sakala field (integration_method) is READ here only, shown next to the setup, never written.
-- sevasindhu_service_id is the one exception: it is also the Seva Sindhu-side service id the "edcs_api" type
-- needs for its token calls, so sp_set_service_sevasindhu_id below lets the integration screen set it.

CREATE OR REPLACE FUNCTION discovery.sp_set_service_sevasindhu_id(p_service_id uuid, p_value text)
RETURNS void
LANGUAGE sql AS $$
    UPDATE discovery.sevaone_service_master
    SET sevasindhu_service_id = nullif(trim(p_value), ''), version = version + 1
    WHERE id = p_service_id;
$$;

CREATE OR REPLACE FUNCTION discovery.sp_list_token_integration_types()
RETURNS TABLE ("TypeKey" varchar, "DisplayName" varchar, "Description" text, "FieldsJson" text)
LANGUAGE sql STABLE AS $$
    SELECT type_key, display_name, description, fields_json::text
    FROM discovery.token_integration_type WHERE is_active ORDER BY display_name;
$$;

-- Search the catalogue for the admin screen. p_search matches name, service code, Sakala/department
-- service id and department name. Configured setups sort first so they are easy to find again.
DROP FUNCTION IF EXISTS discovery.sp_list_token_integration_services(text, text, boolean, integer);
CREATE OR REPLACE FUNCTION discovery.sp_list_token_integration_services(
    p_search text, p_department_code text, p_configured_only boolean, p_citizen_visible_only boolean, p_limit integer
)
RETURNS TABLE (
    "ServiceId" uuid, "DepartmentCode" varchar, "DepartmentName" varchar, "ServiceCode" varchar,
    "Name" varchar, "NameKannada" varchar, "CitizenVisible" boolean, "OperatorMethod" text,
    "TypeKey" varchar, "IsEnabled" boolean, "LandingUrl" varchar)
LANGUAGE sql STABLE AS $$
    SELECT s.id, d.department_id, d.department_name, s.service_id, s.service_name, s.service_name_kannada,
           EXISTS (SELECT 1 FROM discovery.service_role_visibility v JOIN discovery.catalog_role r ON r.id = v.role_id
                   WHERE v.service_id = s.id AND r.role_key = 'citizen'),
           s.integration_method, t.type_key, t.is_enabled, t.landing_url
    FROM discovery.sevaone_service_master s
    JOIN discovery.department_master d ON d.id = s.department_id
    LEFT JOIN discovery.token_integration t ON t.service_id = s.id
    WHERE s.is_active
      AND (p_department_code IS NULL OR p_department_code = '' OR d.department_id = p_department_code)
      AND (NOT COALESCE(p_configured_only, false) OR t.id IS NOT NULL)
      AND (NOT COALESCE(p_citizen_visible_only, false) OR EXISTS (
            SELECT 1 FROM discovery.service_role_visibility v JOIN discovery.catalog_role r ON r.id = v.role_id
            WHERE v.service_id = s.id AND r.role_key = 'citizen'))
      AND (p_search IS NULL OR p_search = '' OR
           s.service_name ILIKE '%' || p_search || '%' OR s.service_id ILIKE '%' || p_search || '%' OR
           COALESCE(s.department_service_id, '') ILIKE '%' || p_search || '%' OR d.department_name ILIKE '%' || p_search || '%')
    ORDER BY (t.id IS NULL), d.department_name, s.service_name
    LIMIT LEAST(GREATEST(COALESCE(p_limit, 50), 1), 200);
$$;

-- One service with its setup (if any). Always returns the service row so the screen can show an
-- empty form; TypeKey is null when nothing is configured yet.
DROP FUNCTION IF EXISTS discovery.sp_get_token_integration(uuid);
CREATE OR REPLACE FUNCTION discovery.sp_get_token_integration(p_service_id uuid)
RETURNS TABLE (
    "ServiceId" uuid, "DepartmentCode" varchar, "DepartmentName" varchar, "ServiceCode" varchar, "Name" varchar,
    "OperatorMethod" text, "OperatorSevasindhuId" text,
    "TypeKey" varchar, "LandingUrl" varchar, "SendMobile" boolean, "SendDigilockerId" boolean,
    "StatusUrl" varchar, "AllowedIps" text[], "ExtraConfig" text, "IsEnabled" boolean,
    "UpdatedAt" timestamptz, "UpdatedBy" text, "Version" integer, "HasStatusSecret" boolean)
LANGUAGE sql STABLE AS $$
    SELECT s.id, d.department_id, d.department_name, s.service_id, s.service_name,
           s.integration_method, s.sevasindhu_service_id,
           t.type_key, t.landing_url, t.send_mobile, t.send_digilocker_id,
           t.status_url, t.allowed_ips, t.extra_config::text, COALESCE(t.is_enabled, false),
           t.updated_at, t.updated_by, t.version, COALESCE(t.status_auth_secret_ciphertext IS NOT NULL, false)
    FROM discovery.sevaone_service_master s
    JOIN discovery.department_master d ON d.id = s.department_id
    LEFT JOIN discovery.token_integration t ON t.service_id = s.id
    WHERE s.id = p_service_id;
$$;

DROP FUNCTION IF EXISTS discovery.sp_upsert_token_integration(uuid, varchar, varchar, boolean, boolean, varchar, text[], text, boolean, text);
CREATE OR REPLACE FUNCTION discovery.sp_upsert_token_integration(
    p_service_id uuid, p_type_key varchar, p_landing_url varchar, p_send_mobile boolean, p_send_digilocker_id boolean,
    p_status_url varchar, p_allowed_ips text[], p_extra_config text, p_is_enabled boolean, p_updated_by text,
    p_status_secret_ciphertext text DEFAULT NULL
)
RETURNS TABLE ("Id" uuid)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    INSERT INTO discovery.token_integration (
        id, service_id, type_key, landing_url, send_mobile, send_digilocker_id, status_url,
        allowed_ips, extra_config, is_enabled, updated_by, status_auth_secret_ciphertext)
    VALUES (
        gen_random_uuid(), p_service_id, p_type_key, NULLIF(p_landing_url, ''), COALESCE(p_send_mobile, false),
        COALESCE(p_send_digilocker_id, false), NULLIF(p_status_url, ''), COALESCE(p_allowed_ips, '{}'),
        COALESCE(NULLIF(p_extra_config, '')::jsonb, '{}'::jsonb), COALESCE(p_is_enabled, false), p_updated_by,
        NULLIF(p_status_secret_ciphertext, ''))
    ON CONFLICT (service_id) DO UPDATE SET
        type_key = EXCLUDED.type_key,
        landing_url = EXCLUDED.landing_url,
        send_mobile = EXCLUDED.send_mobile,
        send_digilocker_id = EXCLUDED.send_digilocker_id,
        status_url = EXCLUDED.status_url,
        allowed_ips = EXCLUDED.allowed_ips,
        extra_config = EXCLUDED.extra_config,
        is_enabled = EXCLUDED.is_enabled,
        status_auth_secret_ciphertext = CASE
            WHEN p_status_secret_ciphertext IS NULL THEN discovery.token_integration.status_auth_secret_ciphertext
            ELSE NULLIF(p_status_secret_ciphertext, '') END,
        updated_at = now(),
        updated_by = EXCLUDED.updated_by,
        version = discovery.token_integration.version + 1
    RETURNING token_integration.id;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('discovery.sp_upsert_token_integration', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;

CREATE OR REPLACE FUNCTION discovery.sp_delete_token_integration(p_service_id uuid)
RETURNS TABLE (deleted boolean)
LANGUAGE plpgsql AS $$
DECLARE n integer;
BEGIN
    DELETE FROM discovery.token_integration WHERE service_id = p_service_id;
    GET DIAGNOSTICS n = ROW_COUNT;
    RETURN QUERY SELECT n > 0;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('discovery.sp_delete_token_integration', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;

-- The enabled setup for a service, or no row. This is what the apply click asks. (Whether the setup is complete
-- for its type is checked when it is saved/enabled, not here.)
DROP FUNCTION IF EXISTS discovery.sp_get_enabled_token_integration(uuid);
CREATE OR REPLACE FUNCTION discovery.sp_get_enabled_token_integration(p_service_id uuid)
RETURNS TABLE ("TypeKey" varchar, "LandingUrl" varchar, "SendMobile" boolean, "SendDigilockerId" boolean, "ExtraConfig" text)
LANGUAGE sql STABLE AS $$
    SELECT t.type_key, t.landing_url, t.send_mobile, t.send_digilocker_id, t.extra_config::text
    FROM discovery.token_integration t
    JOIN discovery.token_integration_type ty ON ty.type_key = t.type_key AND ty.is_active
    WHERE t.service_id = p_service_id AND t.is_enabled;
$$;

-- Starts a handoff: allocates the unique application reference and records what is about to be sent.
-- A sandbox handoff gets the prefix EDCSTEST, and clears out sandbox handoffs older than a week.
DROP FUNCTION IF EXISTS discovery.sp_create_token_integration_attempt(uuid, uuid, uuid, bigint, uuid, boolean, text, text, timestamptz);
DROP FUNCTION IF EXISTS discovery.sp_create_token_integration_attempt(uuid, uuid, uuid, bigint, uuid, boolean, text, text, timestamptz, boolean);
CREATE OR REPLACE FUNCTION discovery.sp_create_token_integration_attempt(
    p_service_id uuid, p_department_id uuid, p_citizen_id uuid, p_sevaone_user_id bigint, p_token_id uuid,
    p_assisted_mode boolean, p_landing_url text, p_sent_fields text, p_expires_at timestamptz,
    p_is_sandbox boolean DEFAULT false,
    p_edcs_ref varchar DEFAULT NULL  -- a reference issued by another system (the Seva Sindhu EP0115 API); null = we allocate one
)
RETURNS TABLE ("EdcsApplicationRefNo" varchar)
LANGUAGE plpgsql AS $$
DECLARE v_ref varchar;
BEGIN
    IF COALESCE(p_is_sandbox, false) THEN
        DELETE FROM discovery.token_integration_attempt WHERE is_sandbox AND created_at < now() - interval '7 days';
    END IF;
    v_ref := COALESCE(NULLIF(p_edcs_ref, ''),
             CASE WHEN COALESCE(p_is_sandbox, false) THEN 'EDCSTEST' ELSE 'EDCS' END
             || nextval('discovery.token_integration_ref_seq')::text);
    INSERT INTO discovery.token_integration_attempt (
        id, edcs_application_ref_no, service_id, department_id, citizen_id, sevaone_user_id, token_id,
        assisted_mode, landing_url, sent_fields, expires_at, is_sandbox)
    VALUES (
        gen_random_uuid(), v_ref, p_service_id, p_department_id, p_citizen_id, p_sevaone_user_id, p_token_id,
        COALESCE(p_assisted_mode, false), p_landing_url, COALESCE(NULLIF(p_sent_fields, '')::jsonb, '{}'::jsonb), p_expires_at,
        COALESCE(p_is_sandbox, false));
    RETURN QUERY SELECT v_ref;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('discovery.sp_create_token_integration_attempt', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;

-- ---- Acknowledgement API (contract URL 2) ------------------------------------------------------

-- The handoff an acknowledgement refers to, with what is needed to check and record it.
DROP FUNCTION IF EXISTS discovery.sp_get_token_integration_attempt(varchar);
CREATE OR REPLACE FUNCTION discovery.sp_get_token_integration_attempt(p_ref varchar)
RETURNS TABLE (
    "AttemptId" uuid, "EdcsApplicationRefNo" varchar, "ServiceId" uuid, "DepartmentId" uuid, "DepartmentCode" varchar,
    "CitizenId" uuid, "SevaOneUserId" bigint, "TokenHash" varchar, "ServiceName" varchar, "SakalaDays" integer,
    "AllowedIps" text[], "Status" varchar, "DepartmentApplicationRefNo" varchar, "IsSandbox" boolean)
LANGUAGE sql STABLE AS $$
    SELECT a.id, a.edcs_application_ref_no, a.service_id, a.department_id, d.department_id, a.citizen_id,
           a.sevaone_user_id, tk.token_hash, s.service_name, s.sakala_days,
           COALESCE(ti.allowed_ips, '{}'), a.status, a.department_application_ref_no, a.is_sandbox
    FROM discovery.token_integration_attempt a
    JOIN discovery.sevaone_service_master s ON s.id = a.service_id
    JOIN discovery.department_master d ON d.id = a.department_id
    LEFT JOIN discovery.token_integration ti ON ti.service_id = a.service_id
    LEFT JOIN portal.sevaone_token tk ON tk.id = a.token_id
    WHERE a.edcs_application_ref_no = p_ref;
$$;

-- Records an acknowledgement. A second identical call (same payload) returns the same update id with
-- AlreadyReceived = true and changes nothing; a different payload for the same application updates it.
CREATE OR REPLACE FUNCTION discovery.sp_apply_token_integration_ack(
    p_attempt_id uuid, p_dept_ref varchar, p_payment_status varchar, p_fee numeric, p_fee_date timestamptz,
    p_payment_ref varchar, p_file_type varchar, p_ack_date timestamptz, p_ack bytea,
    p_payload text, p_payload_hash varchar, p_caller_ip text, p_token_verified boolean
)
RETURNS TABLE ("UpdateId" bigint, "AlreadyReceived" boolean)
LANGUAGE plpgsql AS $$
DECLARE v_id bigint;
BEGIN
    SELECT u.id INTO v_id FROM discovery.token_integration_update u
    WHERE u.attempt_id = p_attempt_id AND u.payload_hash = p_payload_hash;
    IF v_id IS NOT NULL THEN
        RETURN QUERY SELECT v_id, true;
        RETURN;
    END IF;

    INSERT INTO discovery.token_integration_update (attempt_id, payload_hash, payload, caller_ip)
    VALUES (p_attempt_id, p_payload_hash, p_payload::jsonb, p_caller_ip)
    RETURNING id INTO v_id;

    UPDATE discovery.token_integration_attempt SET
        status = 'acknowledged',
        department_application_ref_no = p_dept_ref,
        department_payment_status = p_payment_status,
        department_fee = p_fee,
        department_fee_collection_date = p_fee_date,
        department_payment_ref_number = p_payment_ref,
        acknowledgment_file_type = COALESCE(p_file_type, acknowledgment_file_type),
        acknowledgment_date = COALESCE(p_ack_date, acknowledgment_date),
        acknowledgment = COALESCE(p_ack, acknowledgment),
        acknowledged_at = now(),
        last_payload = p_payload::jsonb,
        token_verified = p_token_verified
    WHERE id = p_attempt_id;

    RETURN QUERY SELECT v_id, false;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('discovery.sp_apply_token_integration_ack', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;

-- The acknowledgement file for a citizen's own application (null columns when none was sent).
CREATE OR REPLACE FUNCTION discovery.sp_get_token_integration_acknowledgement(p_ref varchar, p_citizen_id uuid)
RETURNS TABLE ("FileType" varchar, "FileName" text, "Content" bytea)
LANGUAGE sql STABLE AS $$
    SELECT a.acknowledgment_file_type, a.edcs_application_ref_no, a.acknowledgment
    FROM discovery.token_integration_attempt a
    WHERE a.edcs_application_ref_no = p_ref AND a.citizen_id = p_citizen_id AND a.acknowledgment IS NOT NULL;
$$;

-- ---- Department credentials for the acknowledgement API ---------------------------------------

CREATE OR REPLACE FUNCTION discovery.sp_get_token_integration_credential(p_username varchar)
RETURNS TABLE ("Id" uuid, "DepartmentId" uuid, "DepartmentCode" varchar, "PasswordHash" text, "IsActive" boolean)
LANGUAGE sql STABLE AS $$
    SELECT c.id, c.department_id, d.department_id, c.password_hash, c.is_active
    FROM discovery.token_integration_credential c
    JOIN discovery.department_master d ON d.id = c.department_id
    WHERE c.username = p_username;
$$;

CREATE OR REPLACE FUNCTION discovery.sp_touch_token_integration_credential(p_id uuid)
RETURNS TABLE (ok boolean)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE discovery.token_integration_credential SET last_used_at = now() WHERE id = p_id;
    RETURN QUERY SELECT true;
END;
$$;

CREATE OR REPLACE FUNCTION discovery.sp_list_token_integration_credentials(p_department_id uuid)
RETURNS TABLE ("Id" uuid, "Username" varchar, "IsActive" boolean, "CreatedAt" timestamptz, "CreatedBy" text,
               "PasswordChangedAt" timestamptz, "LastUsedAt" timestamptz)
LANGUAGE sql STABLE AS $$
    SELECT id, username, is_active, created_at, created_by, password_changed_at, last_used_at
    FROM discovery.token_integration_credential WHERE department_id = p_department_id ORDER BY created_at;
$$;

CREATE OR REPLACE FUNCTION discovery.sp_create_token_integration_credential(
    p_department_id uuid, p_username varchar, p_password_hash text, p_created_by text)
RETURNS TABLE ("Id" uuid)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    INSERT INTO discovery.token_integration_credential (id, department_id, username, password_hash, created_by)
    VALUES (gen_random_uuid(), p_department_id, p_username, p_password_hash, p_created_by)
    RETURNING token_integration_credential.id;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('discovery.sp_create_token_integration_credential', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;

CREATE OR REPLACE FUNCTION discovery.sp_update_token_integration_credential(
    p_id uuid, p_department_id uuid, p_password_hash text, p_is_active boolean)
RETURNS TABLE (updated boolean)
LANGUAGE plpgsql AS $$
DECLARE n integer;
BEGIN
    UPDATE discovery.token_integration_credential SET
        password_hash = COALESCE(p_password_hash, password_hash),
        password_changed_at = CASE WHEN p_password_hash IS NULL THEN password_changed_at ELSE now() END,
        is_active = COALESCE(p_is_active, is_active)
    WHERE id = p_id AND department_id = p_department_id;
    GET DIAGNOSTICS n = ROW_COUNT;
    RETURN QUERY SELECT n > 0;
END;
$$;

-- ---- Status check (contract URL 3) --------------------------------------------------------------

-- Everything needed to ask the department about one application. Seva Sindhu applications qualify once the
-- department has acknowledged them; single-sign-on (Kaveri) applications qualify from the moment the citizen is
-- sent. The status address is the service's own, else the type's (provider) one.
DROP FUNCTION IF EXISTS discovery.sp_get_token_status_target(varchar);
CREATE OR REPLACE FUNCTION discovery.sp_get_token_status_target(p_ref varchar)
RETURNS TABLE (
    "AttemptId" uuid, "EdcsApplicationRefNo" varchar, "CitizenId" uuid, "DepartmentApplicationRefNo" varchar,
    "StatusUrl" varchar, "ExtraConfig" text, "StatusSecretCiphertext" text, "StatusCheckedAt" timestamptz,
    "DepartmentStatusText" varchar, "ApplicationId" uuid, "ApplicationStatus" integer, "DepartmentCode" varchar,
    "TypeKey" varchar, "ProviderSettings" text, "ProviderSecretCiphertext" text,
    "ServiceId" uuid, "DepartmentId" uuid, "ServiceName" varchar, "SakalaDays" integer)
LANGUAGE sql STABLE AS $$
    SELECT a.id, a.edcs_application_ref_no, a.citizen_id, a.department_application_ref_no,
           COALESCE(ti.status_url, pr.settings->>'statusUrl')::varchar, ti.extra_config::text, ti.status_auth_secret_ciphertext,
           a.status_checked_at, a.department_status_text, ap.id, ap.status, d.department_id,
           ti.type_key, pr.settings::text, pr.secret_ciphertext,
           a.service_id, a.department_id, s.service_name, s.sakala_days
    FROM discovery.token_integration_attempt a
    JOIN discovery.token_integration ti ON ti.service_id = a.service_id
    JOIN discovery.sevaone_service_master s ON s.id = a.service_id
    JOIN discovery.department_master d ON d.id = a.department_id
    LEFT JOIN discovery.token_integration_provider pr ON pr.type_key = ti.type_key
    LEFT JOIN orchestration.applications ap ON ap.seva_reference = a.edcs_application_ref_no
    WHERE a.edcs_application_ref_no = p_ref
      AND ((a.status = 'acknowledged' AND a.department_application_ref_no IS NOT NULL)
           OR (ti.type_key = 'kaveri_sso' AND a.status IN ('initiated', 'acknowledged')))
      AND COALESCE(ti.status_url, pr.settings->>'statusUrl') IS NOT NULL;
$$;

CREATE OR REPLACE FUNCTION discovery.sp_record_token_status_check(
    p_attempt_id uuid, p_status_text varchar, p_error varchar)
RETURNS TABLE (ok boolean)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE discovery.token_integration_attempt SET
        status_checked_at = now(),
        department_status_text = COALESCE(p_status_text, department_status_text),
        status_check_error = p_error
    WHERE id = p_attempt_id;
    RETURN QUERY SELECT true;
END;
$$;

-- Applications the background check should look at next: acknowledged (or, for single sign-on, started in the last 14
-- days), not finished, with a status address, never checked or not checked for p_older_than. Least recently checked first.
DROP FUNCTION IF EXISTS discovery.sp_list_token_status_due(interval, integer);
CREATE OR REPLACE FUNCTION discovery.sp_list_token_status_due(p_older_than interval, p_limit integer)
RETURNS TABLE ("EdcsApplicationRefNo" varchar)
LANGUAGE sql STABLE AS $$
    SELECT a.edcs_application_ref_no
    FROM discovery.token_integration_attempt a
    JOIN discovery.token_integration ti ON ti.service_id = a.service_id
    LEFT JOIN discovery.token_integration_provider pr ON pr.type_key = ti.type_key
    LEFT JOIN orchestration.applications ap ON ap.seva_reference = a.edcs_application_ref_no
    WHERE COALESCE(ti.status_url, pr.settings->>'statusUrl') IS NOT NULL
      AND ((a.status = 'acknowledged' AND a.department_application_ref_no IS NOT NULL)
           OR (ti.type_key = 'kaveri_sso' AND a.status = 'initiated' AND a.created_at > now() - interval '14 days'))
      AND NOT a.is_sandbox
      AND (ap.id IS NULL OR ap.status NOT IN (3, 4))
      AND (a.status_checked_at IS NULL OR a.status_checked_at < now() - p_older_than)
    ORDER BY a.status_checked_at NULLS FIRST
    LIMIT LEAST(GREATEST(COALESCE(p_limit, 20), 1), 100);
$$;

-- What the dashboard needs to know about a citizen's own application, without loading any file.
DROP FUNCTION IF EXISTS discovery.sp_has_token_integration_acknowledgement(varchar, uuid);
CREATE OR REPLACE FUNCTION discovery.sp_get_token_application_flags(p_ref varchar, p_citizen_id uuid)
RETURNS TABLE ("HasAcknowledgement" boolean, "StatusCheckAvailable" boolean)
LANGUAGE sql STABLE AS $$
    SELECT COALESCE(bool_or(a.acknowledgment IS NOT NULL), false),
           COALESCE(bool_or(COALESCE(ti.status_url, pr.settings->>'statusUrl') IS NOT NULL
                            AND ((a.status = 'acknowledged' AND a.department_application_ref_no IS NOT NULL) OR ti.type_key = 'kaveri_sso')), false)
    FROM discovery.token_integration_attempt a
    LEFT JOIN discovery.token_integration ti ON ti.service_id = a.service_id
    LEFT JOIN discovery.token_integration_provider pr ON pr.type_key = ti.type_key
    WHERE a.edcs_application_ref_no = p_ref AND a.citizen_id = p_citizen_id;
$$;

-- The encrypted secret for a service's status API, for the admin "test" call only.
CREATE OR REPLACE FUNCTION discovery.sp_get_token_status_secret(p_service_id uuid)
RETURNS text
LANGUAGE sql STABLE AS $$
    SELECT status_auth_secret_ciphertext FROM discovery.token_integration WHERE service_id = p_service_id;
$$;

-- ---- Provider (per-type) settings and the attempt finish step --------------------------------------

CREATE OR REPLACE FUNCTION discovery.sp_get_token_provider(p_type_key varchar)
RETURNS TABLE ("TypeKey" varchar, "SettingsJson" text, "SecretCiphertext" text, "UpdatedAt" timestamptz, "UpdatedBy" text)
LANGUAGE sql STABLE AS $$
    SELECT type_key, settings::text, secret_ciphertext, updated_at, updated_by
    FROM discovery.token_integration_provider WHERE type_key = p_type_key;
$$;

-- p_secret_ciphertext: NULL keeps the stored secret, '' removes it, anything else replaces it.
CREATE OR REPLACE FUNCTION discovery.sp_save_token_provider(p_type_key varchar, p_settings text, p_secret_ciphertext text, p_updated_by text)
RETURNS TABLE (ok boolean)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO discovery.token_integration_provider (type_key, settings, secret_ciphertext, updated_by)
    VALUES (p_type_key, COALESCE(NULLIF(p_settings, '')::jsonb, '{}'::jsonb), NULLIF(p_secret_ciphertext, ''), p_updated_by)
    ON CONFLICT (type_key) DO UPDATE SET
        settings = COALESCE(NULLIF(p_settings, '')::jsonb, '{}'::jsonb),
        secret_ciphertext = CASE WHEN p_secret_ciphertext IS NULL THEN discovery.token_integration_provider.secret_ciphertext
                                 ELSE NULLIF(p_secret_ciphertext, '') END,
        updated_at = now(),
        updated_by = p_updated_by;
    RETURN QUERY SELECT true;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('discovery.sp_save_token_provider', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;

-- After the handoff call: where the citizen was sent (without any session token), and whether it worked.
CREATE OR REPLACE FUNCTION discovery.sp_finish_token_integration_attempt(p_attempt_id uuid, p_landing_url text, p_status varchar, p_error varchar)
RETURNS TABLE (ok boolean)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE discovery.token_integration_attempt SET
        landing_url = COALESCE(p_landing_url, landing_url),
        status = p_status,
        status_check_error = p_error
    WHERE id = p_attempt_id;
    RETURN QUERY SELECT true;
END;
$$;

-- Allocates the reference (sp_create_token_integration_attempt) needs the attempt row; this variant lets a handoff
-- that does not have its landing address yet (single sign-on) record the attempt first.

-- ---- Token check (the department verifies the token it received) --------------------------------

-- What the token check needs: the handoff, the stored token fingerprint, and the few user details the service is set to share.
CREATE OR REPLACE FUNCTION discovery.sp_get_token_verify_info(p_ref varchar)
RETURNS TABLE (
    "AttemptId" uuid, "EdcsApplicationRefNo" varchar, "DepartmentId" uuid, "DepartmentCode" varchar, "SevaOneUserId" bigint,
    "AssistedMode" boolean, "ExpiresAt" timestamptz, "IsSandbox" boolean, "TokenHash" varchar, "TokenRevoked" boolean,
    "CitizenName" varchar, "CitizenMobile" varchar, "CitizenDigilockerId" varchar,
    "SendMobile" boolean, "SendDigilockerId" boolean, "ServiceValue" varchar, "TokenVerifiedAt" timestamptz)
LANGUAGE sql STABLE AS $$
    SELECT a.id, a.edcs_application_ref_no, a.department_id, d.department_id, a.sevaone_user_id,
           a.assisted_mode, a.expires_at, a.is_sandbox, tk.token_hash, (tk.revoked_at IS NOT NULL OR tk.is_active = false),
           c.name, c.mobile, c.digilocker_id,
           COALESCE(ti.send_mobile, false), COALESCE(ti.send_digilocker_id, false),
           COALESCE(NULLIF(s.department_service_id, ''), s.service_id)::varchar, a.token_verified_at
    FROM discovery.token_integration_attempt a
    JOIN discovery.department_master d ON d.id = a.department_id
    JOIN discovery.sevaone_service_master s ON s.id = a.service_id
    JOIN portal.sevaone_user c ON c.id = a.citizen_id
    LEFT JOIN discovery.token_integration ti ON ti.service_id = a.service_id
    LEFT JOIN portal.sevaone_token tk ON tk.id = a.token_id
    WHERE a.edcs_application_ref_no = p_ref;
$$;

-- Records the first successful token check; tells the caller whether it was a repeat.
CREATE OR REPLACE FUNCTION discovery.sp_mark_token_verified(p_attempt_id uuid)
RETURNS TABLE ("AlreadyVerified" boolean)
LANGUAGE plpgsql AS $$
DECLARE v_before timestamptz;
BEGIN
    SELECT token_verified_at INTO v_before FROM discovery.token_integration_attempt WHERE id = p_attempt_id;
    UPDATE discovery.token_integration_attempt SET token_verified_at = COALESCE(token_verified_at, now()) WHERE id = p_attempt_id;
    RETURN QUERY SELECT v_before IS NOT NULL;
END;
$$;

-- ---- Pull-back of acknowledgements held by the Seva Sindhu API ---------------------------------------
-- Applications started with the "edcs_api" type that have no acknowledgement yet (started in the last 7 days, not asked for
-- p_older_than), least recently asked first.
DROP FUNCTION IF EXISTS discovery.sp_list_token_ack_due(interval, integer);
CREATE OR REPLACE FUNCTION discovery.sp_list_token_ack_due(p_older_than interval, p_limit integer)
RETURNS TABLE ("AttemptId" uuid, "EdcsApplicationRefNo" varchar, "SevaOneUserId" bigint, "DepartmentId" uuid,
               "DepartmentCode" varchar, "SevasindhuServiceId" text)
LANGUAGE sql STABLE AS $$
    SELECT a.id, a.edcs_application_ref_no, a.sevaone_user_id, a.department_id, d.department_id::varchar, s.sevasindhu_service_id
    FROM discovery.token_integration_attempt a
    JOIN discovery.token_integration ti ON ti.service_id = a.service_id
    JOIN discovery.sevaone_service_master s ON s.id = a.service_id
    JOIN discovery.department_master d ON d.id = a.department_id
    WHERE ti.type_key = 'edcs_api'
      AND a.status = 'initiated'
      AND NOT a.is_sandbox
      -- Only references the Seva Sindhu API issued ("EDCS" + its service id + digits). Our own local references use a different
      -- numbering that can coincide with other applications' references over there, so they must never be looked up.
      AND a.sent_fields ? 'edcsApplicationRefNo'
      AND s.sevasindhu_service_id IS NOT NULL
      AND a.edcs_application_ref_no LIKE 'EDCS' || s.sevasindhu_service_id || '%' 
      AND a.created_at > now() - interval '7 days'
      AND (a.ack_checked_at IS NULL OR a.ack_checked_at < now() - p_older_than)
    ORDER BY a.ack_checked_at NULLS FIRST
    LIMIT LEAST(GREATEST(COALESCE(p_limit, 20), 1), 100);
$$;

CREATE OR REPLACE FUNCTION discovery.sp_record_token_ack_check(p_attempt_id uuid)
RETURNS void
LANGUAGE sql AS $$
    UPDATE discovery.token_integration_attempt SET ack_checked_at = now() WHERE id = p_attempt_id;
$$;
