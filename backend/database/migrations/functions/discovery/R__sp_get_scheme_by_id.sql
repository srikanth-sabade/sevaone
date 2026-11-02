-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

-- Explicit DROP: return row shape changed (V28/V30/V31 -- service_master fee/documents/deeplink
-- columns), which Postgres refuses to apply via CREATE OR REPLACE alone.
DROP FUNCTION IF EXISTS discovery.sp_get_scheme_by_id(uuid);

CREATE OR REPLACE FUNCTION discovery.sp_get_scheme_by_id(p_id uuid)
RETURNS TABLE (
    "Id" uuid, "ServiceCode" character varying(32), "DepartmentId" uuid, "Name" character varying(300),
    "Category" text, "SakalaDays" integer, "Description" text,
    "DepartmentFee" numeric(12,2), "ServiceCharge" numeric(12,2), "RequiredDocumentsJson" text,
    "EligibilityJson" text, "AliasesJson" text, "StepsJson" text, "Portal" text,
    "ServiceUrl" character varying(500), "DeeplinkCitizenUrl" character varying(500),
    "DeeplinkOperatorUrl" character varying(500), "HomepageUrl" character varying(500),
    "IsActive" boolean, "CreatedAt" timestamp with time zone
)
LANGUAGE sql STABLE AS $$
    SELECT id, service_id, department_id, service_name, category, sakala_days, description,
           department_fee, service_charge,
           required_documents_json, eligibility_json, aliases_json, steps_json, portal, service_url,
           deeplink_citizen_url, deeplink_operator_url, homepage_url, is_active, created_at
    FROM discovery.sevaone_service_master
    WHERE id = p_id;
$$;
