-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- Hydrates an entire department page in one call: every service visible to p_role_key,
-- with its sub-department name attached (NULL for ungrouped services). The frontend groups
-- client-side, same as it already does against the static generated dataset today.

-- DROP FUNCTION IF EXISTS is required here (not just CREATE OR REPLACE): the return row shape
-- changed (added IntegrationMethod) -- see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
DROP FUNCTION IF EXISTS discovery.sp_list_services_by_department_for_role(uuid, character varying);

CREATE OR REPLACE FUNCTION discovery.sp_list_services_by_department_for_role(p_department_id uuid, p_role_key character varying(64))
RETURNS TABLE (
    "Id" uuid, "ServiceId" character varying(32), "SubDepartmentId" uuid, "SubDepartmentName" character varying(200),
    "Name" character varying(300), "NameKannada" character varying(300),
    "ShortDescription" text, "Description" text, "DescriptionKannada" text,
    "IsSakalaEnabled" boolean, "SakalaLabelRaw" text, "SakalaDays" integer,
    "DepartmentFee" numeric(12,2), "ServiceCharge" numeric(12,2),
    "DocumentsJson" text, "DocumentsJsonKannada" text,
    "EligibilityJson" text, "EligibilityJsonKannada" text,
    "StepsJson" text, "StepsJsonKannada" text,
    "Jurisdiction" character varying(100), "Category" text, "ModeOfService" character varying(20),
    "ApplyServiceUrl" character varying(500), "ApplyServiceUrlKannada" character varying(500), "ServiceUrl" character varying(500),
    "IntegrationMethod" text
)
LANGUAGE sql STABLE AS $$
    SELECT
        s.id, s.service_id, s.sub_department_id, sd.sub_department_name,
        s.service_name, s.service_name_kannada, s.short_description, s.description, s.description_kannada,
        s.is_sakala_enabled, s.sakala_label_raw, s.sakala_days,
        s.department_fee, s.service_charge,
        s.required_documents_json, s.documents_json_kannada,
        s.eligibility_json, s.eligibility_json_kannada, s.steps_json, s.steps_json_kannada,
        s.jurisdiction, s.category, s.mode_of_service, s.deeplink_citizen_url, s.deeplink_citizen_url_kannada, s.service_url,
        s.integration_method
    FROM discovery.sevaone_service_master s
    LEFT JOIN discovery.sub_department_master sd ON sd.id = s.sub_department_id
    JOIN discovery.service_role_visibility v ON v.service_id = s.id
    JOIN discovery.catalog_role r ON r.id = v.role_id
    JOIN discovery.department_master d ON d.id = s.department_id
    WHERE s.department_id = p_department_id AND s.is_active = TRUE AND d.is_active = TRUE AND r.role_key = p_role_key
    ORDER BY sd.display_order NULLS LAST, s.service_name;
$$;
