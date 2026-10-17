-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

-- Explicit DROP: return row shape changed (V22/V25 -- department_master url column; V28/V30/V31
-- -- service_master fee/documents/deeplink columns), which Postgres refuses to apply via
-- CREATE OR REPLACE alone.
DROP FUNCTION IF EXISTS discovery.sp_get_scheme_with_department_by_service_code(text);

CREATE OR REPLACE FUNCTION discovery.sp_get_scheme_with_department_by_service_code(p_service_code text)
RETURNS TABLE (
    "ServiceCode" character varying(32), "Name" character varying(300), "DepartmentCode" character varying(64),
    "DepartmentName" character varying(200), "Category" text, "SakalaDays" integer,
    "DepartmentFee" numeric(12,2), "ServiceCharge" numeric(12,2),
    "Description" text, "RequiredDocumentsJson" text, "EligibilityJson" text, "AliasesJson" text, "StepsJson" text,
    "Portal" text, "DepartmentIntegrated" boolean
)
LANGUAGE sql STABLE AS $$
    SELECT s.service_id, s.service_name, d.department_id, d.department_name, s.category, s.sakala_days,
           s.department_fee, s.service_charge, s.description, s.required_documents_json, s.eligibility_json, s.aliases_json, s.steps_json,
           s.portal, d.is_integrated
    FROM discovery.sevaone_service_master s
    JOIN discovery.department_master d ON d.id = s.department_id
    WHERE s.service_id = p_service_code
    LIMIT 1;
$$;
