-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
--
-- Backs GET /api/v1/admin/gov/analytics/overview and .../citizens (GovDashboardService composes
-- both endpoint DTOs from this one call). p_district/p_taluk: NULL means no filter (statewide
-- tiers); a non-null value scopes to it (District/Taluk Head) -- see GovDashboardService, the
-- only place that decides which value to pass; this function trusts whatever it's given.
-- Aggregates only -- never selects a PII column (name/mobile/address/aadhaar), by construction.
CREATE OR REPLACE FUNCTION portal.sp_get_gov_citizen_totals(p_district text DEFAULT NULL, p_taluk text DEFAULT NULL)
RETURNS TABLE (registered_citizens bigint, profile_completed_citizens bigint)
LANGUAGE sql STABLE AS $$
    SELECT count(*) AS registered_citizens,
           count(*) FILTER (WHERE u.profile_completed) AS profile_completed_citizens
    FROM portal.sevaone_user u
    WHERE (p_district IS NULL OR lower(trim(u.district)) = lower(trim(p_district)))
      AND (p_taluk IS NULL OR lower(trim(u.taluk)) = lower(trim(p_taluk)));
$$;
