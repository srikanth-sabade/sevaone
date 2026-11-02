-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- Bulk export for the AI backend's catalog ingestion (see ai_backend/app/ingestion/): every
-- service actually visible to citizens, across every department, in one call -- the same
-- role-visibility rule the /catalog page itself uses (discovery.service_role_visibility),
-- hardcoded to 'citizen' here rather than taking a role parameter, since this export only
-- ever serves the citizen-facing AI assistant, never operator/admin views.

CREATE OR REPLACE FUNCTION discovery.sp_list_services_for_ai_export()
RETURNS TABLE (
    "Id" uuid, "ServiceId" character varying(32),
    "DepartmentId" uuid, "DepartmentCode" character varying(64), "DepartmentName" character varying(200),
    "SubDepartmentId" uuid, "SubDepartmentName" character varying(200),
    "Name" character varying(300), "NameKannada" character varying(300),
    "ShortDescription" text, "Description" text, "DescriptionKannada" text,
    "IsSakalaEnabled" boolean, "SakalaLabelRaw" text, "SakalaDays" integer,
    "DepartmentFee" numeric(12,2), "ServiceCharge" numeric(12,2),
    "DocumentsJson" text, "DocumentsJsonKannada" text,
    "EligibilityJson" text, "EligibilityJsonKannada" text,
    "StepsJson" text, "StepsJsonKannada" text,
    "AliasesJson" text,
    "Jurisdiction" character varying(100), "Category" text, "ModeOfService" character varying(20),
    "ApplyServiceUrl" character varying(500), "ApplyServiceUrlKannada" character varying(500), "ServiceUrl" character varying(500)
)
LANGUAGE sql STABLE AS $$
    SELECT
        s.id, s.service_id,
        d.id, d.department_id, d.department_name,
        s.sub_department_id, sd.sub_department_name,
        s.service_name, s.service_name_kannada, s.short_description, s.description, s.description_kannada,
        s.is_sakala_enabled, s.sakala_label_raw, s.sakala_days,
        s.department_fee, s.service_charge,
        s.required_documents_json, s.documents_json_kannada,
        s.eligibility_json, s.eligibility_json_kannada, s.steps_json, s.steps_json_kannada,
        s.aliases_json,
        s.jurisdiction, s.category, s.mode_of_service, s.deeplink_citizen_url, s.deeplink_citizen_url_kannada, s.service_url
    FROM discovery.sevaone_service_master s
    JOIN discovery.department_master d ON d.id = s.department_id
    LEFT JOIN discovery.sub_department_master sd ON sd.id = s.sub_department_id
    JOIN discovery.service_role_visibility v ON v.service_id = s.id
    JOIN discovery.catalog_role r ON r.id = v.role_id AND r.role_key = 'citizen'
    WHERE s.is_active = TRUE AND d.is_active = TRUE
    ORDER BY d.display_order NULLS LAST, sd.display_order NULLS LAST, s.service_name;
$$;
