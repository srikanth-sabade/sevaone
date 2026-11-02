-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

-- Explicit DROP: return row shape changed (V26/V27 -- department_onboarding cleanup, nodal
-- officer name), which Postgres refuses to apply via CREATE OR REPLACE alone.
DROP FUNCTION IF EXISTS discovery.sp_get_department_onboarding_by_department(uuid);

CREATE OR REPLACE FUNCTION discovery.sp_get_department_onboarding_by_department(p_department_id uuid)
RETURNS TABLE (
    "Id" uuid, "DepartmentId" uuid, "ContactPersonName" character varying(200),
    "Designation" character varying(200), "ContactNumber" character varying(15),
    "Email" character varying(200), "CcEmails" text,
    "NodalOfficerNominationRequestedAt" timestamp with time zone, "NodalOfficerName" character varying(200),
    "AiEnabled" boolean, "OnboardingStage" character varying(30),
    "CreatedAt" timestamp with time zone, "UpdatedAt" timestamp with time zone
)
LANGUAGE sql STABLE AS $$
    SELECT id, department_id, contact_person_name, designation, contact_number, email, cc_emails,
           nodal_officer_nomination_requested_at, nodal_officer_name, ai_enabled, onboarding_stage, created_at, updated_at
    FROM discovery.department_onboarding
    WHERE department_id = p_department_id;
$$;
