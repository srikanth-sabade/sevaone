-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- Natural key is (department_id, service_id) -- ix_sevaone_service_master_department_id_service_code
-- already enforces this; service_id is only unique *within* a department in real source
-- data (confirmed: the same code means a different service under a different department).
-- Deliberately leaves aliases_json/portal untouched on both insert and conflict -- aliases
-- are curated separately from the source spreadsheet, not something ingestion owns.
-- No fee/is_mandatory params here -- both columns were dropped from this table (develop's
-- own V28, merged in after this function was first written): fee was legacy free text
-- superseded by department_fee/service_charge, and is_mandatory at the service level
-- duplicated the (correct) per-document concept on sevaone_service_document.
-- documents_json/documents_json_kannada (required_documents_json at the column level, per
-- develop's V30 rename) ARE written here -- a "Required Documents" checklist has no URL at
-- all, a different concept from sevaone_service_document's per-row document_url (NOT NULL,
-- reserved for sources that actually provide one; see sp_upsert_service_document for that
-- path instead).
-- p_mode_of_service must already be normalized to 'Online'/'Offline'/'Both' by the caller --
-- the CHECK constraint on that column rejects anything else, and real source data has been
-- seen as a combined "Online/Offline" string that needs mapping to 'Both' before this call.

CREATE OR REPLACE FUNCTION discovery.sp_upsert_sevaone_service(
    p_id uuid, p_service_id character varying(32), p_department_id uuid, p_sub_department_id uuid,
    p_service_name character varying(300), p_service_name_kannada character varying(300),
    p_category text, p_sakala_days integer, p_is_sakala_enabled boolean, p_sakala_label_raw text,
    p_description text, p_description_kannada text, p_short_description text,
    p_department_fee numeric(12,2), p_service_charge numeric(12,2),
    p_documents_json text, p_documents_json_kannada text,
    p_eligibility_json text, p_eligibility_json_kannada text,
    p_steps_json text, p_steps_json_kannada text,
    p_jurisdiction character varying(100), p_mode_of_service character varying(20),
    p_department_service_id character varying(64),
    p_service_url character varying(500), p_deeplink_citizen_url character varying(500),
    p_deeplink_citizen_url_kannada character varying(500)
)
RETURNS TABLE ("Id" uuid)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    INSERT INTO discovery.sevaone_service_master (
        id, service_id, department_id, sub_department_id, service_name, service_name_kannada,
        category, sakala_days, is_sakala_enabled, sakala_label_raw, description, description_kannada, short_description,
        department_fee, service_charge, required_documents_json, documents_json_kannada,
        eligibility_json, eligibility_json_kannada, steps_json, steps_json_kannada,
        jurisdiction, mode_of_service,
        department_service_id, service_url, deeplink_citizen_url, deeplink_citizen_url_kannada, is_active, version
    )
    VALUES (
        p_id, p_service_id, p_department_id, p_sub_department_id, p_service_name, p_service_name_kannada,
        COALESCE(p_category, 'General'), COALESCE(p_sakala_days, 0), p_is_sakala_enabled, p_sakala_label_raw,
        COALESCE(p_description, ''), p_description_kannada, p_short_description,
        p_department_fee, p_service_charge, COALESCE(p_documents_json, '[]'), COALESCE(p_documents_json_kannada, '[]'),
        COALESCE(p_eligibility_json, '[]'), COALESCE(p_eligibility_json_kannada, '[]'),
        COALESCE(p_steps_json, '[]'), COALESCE(p_steps_json_kannada, '[]'), p_jurisdiction, p_mode_of_service,
        p_department_service_id, p_service_url, p_deeplink_citizen_url, p_deeplink_citizen_url_kannada, TRUE, 1
    )
    ON CONFLICT (department_id, service_id) DO UPDATE SET
        sub_department_id = EXCLUDED.sub_department_id,
        service_name = EXCLUDED.service_name,
        service_name_kannada = EXCLUDED.service_name_kannada,
        category = EXCLUDED.category,
        sakala_days = EXCLUDED.sakala_days,
        is_sakala_enabled = EXCLUDED.is_sakala_enabled,
        sakala_label_raw = EXCLUDED.sakala_label_raw,
        description = EXCLUDED.description,
        description_kannada = EXCLUDED.description_kannada,
        short_description = EXCLUDED.short_description,
        department_fee = EXCLUDED.department_fee,
        service_charge = EXCLUDED.service_charge,
        required_documents_json = EXCLUDED.required_documents_json,
        documents_json_kannada = EXCLUDED.documents_json_kannada,
        eligibility_json = EXCLUDED.eligibility_json,
        eligibility_json_kannada = EXCLUDED.eligibility_json_kannada,
        steps_json = EXCLUDED.steps_json,
        steps_json_kannada = EXCLUDED.steps_json_kannada,
        jurisdiction = EXCLUDED.jurisdiction,
        mode_of_service = EXCLUDED.mode_of_service,
        department_service_id = EXCLUDED.department_service_id,
        service_url = EXCLUDED.service_url,
        deeplink_citizen_url = EXCLUDED.deeplink_citizen_url,
        deeplink_citizen_url_kannada = EXCLUDED.deeplink_citizen_url_kannada,
        is_active = TRUE,
        version = discovery.sevaone_service_master.version + 1
    RETURNING id;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('discovery.sp_upsert_sevaone_service', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
