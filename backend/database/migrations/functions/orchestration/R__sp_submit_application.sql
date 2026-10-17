-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION orchestration.sp_submit_application(
    p_id uuid, p_seva_reference character varying(32), p_department_reference text, p_citizen_id uuid,
    p_scheme_id uuid, p_department_id uuid, p_status integer, p_submitted_at timestamptz,
    p_sakala_due_at timestamptz, p_applicant_name text, p_address text, p_purpose text,
    p_district text, p_taluk text, p_event_id uuid, p_event_note text, p_event_at timestamptz
)
RETURNS TABLE (success boolean, message text)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO orchestration.applications
        (id, seva_reference, department_reference, citizen_id, scheme_id, department_id, status,
         submitted_at, sakala_due_at, applicant_name, address, purpose, district, taluk)
    VALUES
        (p_id, p_seva_reference, p_department_reference, p_citizen_id, p_scheme_id, p_department_id, p_status,
         p_submitted_at, p_sakala_due_at, p_applicant_name, p_address, p_purpose, p_district, p_taluk);

    INSERT INTO status.application_events (id, application_id, status, note, at)
    VALUES (p_event_id, p_id, p_status, p_event_note, p_event_at);

    RETURN QUERY SELECT TRUE, 'Application submitted.'::text;
EXCEPTION WHEN OTHERS THEN
    INSERT INTO audit.db_errors (proc_name, sql_state, error_message)
    VALUES ('orchestration.sp_submit_application', SQLSTATE, SQLERRM);
    RAISE;
END;
$$;
