-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

-- Explicit DROP: return row shape changed (V28/V30/V31 -- service_master fee/documents/deeplink
-- columns), which Postgres refuses to apply via CREATE OR REPLACE alone.
DROP FUNCTION IF EXISTS discovery.sp_list_schemes_with_department(text, text, boolean);

CREATE OR REPLACE FUNCTION discovery.sp_list_schemes_with_department(
    p_department_code text, p_category text, p_only_active boolean
)
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
    WHERE (p_only_active = FALSE OR (s.is_active = TRUE AND d.is_active = TRUE))
      AND (p_department_code IS NULL OR d.department_id = p_department_code)
      AND (p_category IS NULL OR s.category = p_category)
    ORDER BY s.service_id;
$$;
