-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- Backs the landing page's "Frequently requested this month" section (previously an arbitrary
-- .slice(0, N) of the plain catalog list -- not based on real usage at all). Ranks citizen-
-- visible, active services by real apply attempts (discovery.service_apply_track) since
-- p_since, most-requested first.
--
-- Three-level ordering:
--   1. real apply count in the window, desc -- genuine demand always wins;
--   2. a small curated list of the services citizens most commonly need, so a freshly
--      imported catalog with no apply history yet shows a sensible list (Income / Caste /
--      RTC / Ration card ...) instead of whatever sorts first alphabetically ("1st
--      Anaesthesia and Operation Theatre Technology");
--   3. service name, so the tail past the curated list is still deterministic.
-- The section always returns p_limit rows -- never fewer, never fabricated counts. As real
-- apply data accumulates it naturally takes over from the curated seed.
--
-- Curated rows are matched by (department code, exact service name); a name that does not
-- exist in this environment's catalog import simply contributes nothing (LEFT JOIN), so the
-- list degrades to alphabetical for that slot rather than erroring.
--
-- Same row shape as sp_list_schemes_with_department (reuses SchemeWithDepartment /
-- SchemeSummaryDto and its existing AutoMapper mapping) plus three appended count columns,
-- broken down by discovery.service_apply_track.source (see V67) rather than one blended
-- ApplyCount - CatalogueApplyCount and AiApplyCount always sum to TotalApplyCount.
--
-- DROP FUNCTION IF EXISTS is required here (not just CREATE OR REPLACE): the return row shape
-- changed (one ApplyCount column became three) -- see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
DROP FUNCTION IF EXISTS discovery.sp_list_top_requested_services(timestamptz, integer);

CREATE OR REPLACE FUNCTION discovery.sp_list_top_requested_services(p_since timestamptz, p_limit integer)
RETURNS TABLE (
    "ServiceCode" character varying(32), "Name" character varying(300), "DepartmentCode" character varying(64),
    "DepartmentName" character varying(200), "Category" text, "SakalaDays" integer,
    "DepartmentFee" numeric(12,2), "ServiceCharge" numeric(12,2),
    "Description" text, "RequiredDocumentsJson" text, "EligibilityJson" text, "AliasesJson" text, "StepsJson" text,
    "Portal" text, "DepartmentIntegrated" boolean,
    "CatalogueApplyCount" bigint, "AiApplyCount" bigint, "TotalApplyCount" bigint
)
LANGUAGE sql STABLE AS $$
    WITH curated (department_code, service_name, rank) AS (
        VALUES
            ('revenue',   'Income Certificate',                                1),
            ('revenue',   'Caste Certificate (SC/ST)',                         2),
            ('revenue',   'Residence Certificate',                             3),
            ('revenue',   'RTC (Record of Rights, Tenancy and Crops) Print',   4),
            ('revenue',   'Issue of Encumbrance Certificate',                  5),
            ('revenue',   'Mutation Extract',                                  6),
            ('food',      'New Ration Card',                                   7),
            ('health',    'Birth Certificate issued in Hospital',              8),
            ('wcd',       'Gruha Lakshmi',                                     9),
            ('transport', 'Driving License',                                  10)
    )
    SELECT
        s.service_id, s.service_name, d.department_id, d.department_name, s.category, s.sakala_days,
        s.department_fee, s.service_charge, s.description, s.required_documents_json, s.eligibility_json, s.aliases_json, s.steps_json,
        s.portal, d.is_integrated,
        count(t.id) FILTER (WHERE t.created_at >= p_since AND t.source = 'catalogue') AS catalogue_apply_count,
        count(t.id) FILTER (WHERE t.created_at >= p_since AND t.source = 'ai') AS ai_apply_count,
        count(t.id) FILTER (WHERE t.created_at >= p_since) AS total_apply_count
    FROM discovery.sevaone_service_master s
    JOIN discovery.department_master d ON d.id = s.department_id
    JOIN discovery.service_role_visibility v ON v.service_id = s.id
    JOIN discovery.catalog_role r ON r.id = v.role_id AND r.role_key = 'citizen'
    LEFT JOIN discovery.service_apply_track t ON t.scheme_id = s.id
    LEFT JOIN curated c ON c.department_code = d.department_id AND c.service_name = s.service_name
    WHERE s.is_active = TRUE AND d.is_active = TRUE
    GROUP BY s.id, d.id, d.department_id, d.department_name, s.service_id, s.service_name, s.category, s.sakala_days,
             s.department_fee, s.service_charge, s.description, s.required_documents_json, s.eligibility_json, s.aliases_json, s.steps_json, s.portal,
             c.rank
    ORDER BY total_apply_count DESC, c.rank NULLS LAST, s.service_name
    LIMIT p_limit;
$$;
