-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- Unlike dept_user_ref's upsert, this one really does update on conflict -- onboarding status
-- (ai_enabled, onboarding_stage) changes repeatedly as EDCS moves a department through the
-- rollout, not a write-once reference row.

CREATE OR REPLACE FUNCTION discovery.sp_upsert_department_onboarding(
    p_id uuid, p_department_id uuid, p_contact_person_name character varying(200),
    p_designation character varying(200), p_contact_number character varying(15),
    p_email character varying(200), p_cc_emails text,
    p_nodal_officer_nomination_requested_at timestamptz, p_nodal_officer_name character varying(200),
    p_ai_enabled boolean, p_onboarding_stage character varying(30),
    p_created_at timestamptz, p_updated_at timestamptz
)
RETURNS TABLE (success boolean, message text)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO discovery.department_onboarding (
        id, department_id, contact_person_name, designation, contact_number, email, cc_emails,
        nodal_officer_nomination_requested_at, nodal_officer_name, ai_enabled, onboarding_stage, created_at, updated_at
    )
    VALUES (
        p_id, p_department_id, p_contact_person_name, p_designation, p_contact_number, p_email, p_cc_emails,
        p_nodal_officer_nomination_requested_at, p_nodal_officer_name, p_ai_enabled, p_onboarding_stage, p_created_at, p_updated_at
    )
    ON CONFLICT (department_id) DO UPDATE SET
        contact_person_name = EXCLUDED.contact_person_name,
        designation = EXCLUDED.designation,
        contact_number = EXCLUDED.contact_number,
        email = EXCLUDED.email,
        cc_emails = EXCLUDED.cc_emails,
        nodal_officer_nomination_requested_at = EXCLUDED.nodal_officer_nomination_requested_at,
        nodal_officer_name = EXCLUDED.nodal_officer_name,
        ai_enabled = EXCLUDED.ai_enabled,
        onboarding_stage = EXCLUDED.onboarding_stage,
        updated_at = EXCLUDED.updated_at;
    RETURN QUERY SELECT TRUE, 'Department onboarding status recorded.'::text;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('discovery.sp_upsert_department_onboarding', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
