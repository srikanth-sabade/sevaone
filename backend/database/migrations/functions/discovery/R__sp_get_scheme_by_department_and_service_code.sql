-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- Replaces sp_get_scheme_by_service_code (dropped in V39): service_id is only unique within a
-- department, not globally, so every caller (apply-token minting, application submit, the
-- department-completion webhook) must pass the department it already has instead of looking
-- up by service_id alone.

-- DROP FUNCTION IF EXISTS is required here (not just CREATE OR REPLACE): the return row shape
-- changed (added IntegrationMethod) -- see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
DROP FUNCTION IF EXISTS discovery.sp_get_scheme_by_department_and_service_code(character varying, character varying);

CREATE OR REPLACE FUNCTION discovery.sp_get_scheme_by_department_and_service_code(
    p_department_code character varying(64), p_service_code character varying(32))
RETURNS TABLE (
    "Id" uuid, "ServiceCode" character varying(32), "DepartmentId" uuid, "Name" character varying(300),
    "Category" text, "SakalaDays" integer, "Description" text,
    "DepartmentFee" numeric(12,2), "ServiceCharge" numeric(12,2), "RequiredDocumentsJson" text,
    "EligibilityJson" text, "AliasesJson" text, "StepsJson" text, "Portal" text,
    "ServiceUrl" character varying(500), "DeeplinkCitizenUrl" character varying(500),
    "DeeplinkOperatorUrl" character varying(500), "HomepageUrl" character varying(500),
    "IntegrationMethod" text, "SevasindhuServiceId" text, "IsActive" boolean, "CreatedAt" timestamp with time zone
)
LANGUAGE sql STABLE AS $$
    SELECT s.id, s.service_id, s.department_id, s.service_name, s.category, s.sakala_days, s.description,
           s.department_fee, s.service_charge,
           s.required_documents_json, s.eligibility_json, s.aliases_json, s.steps_json, s.portal, s.service_url,
           s.deeplink_citizen_url, s.deeplink_operator_url, s.homepage_url, s.integration_method, s.sevasindhu_service_id,
           s.is_active, s.created_at
    FROM discovery.sevaone_service_master s
    JOIN discovery.department_master d ON d.id = s.department_id
    WHERE d.department_id = p_department_code AND s.service_id = p_service_code
    LIMIT 1;
$$;
