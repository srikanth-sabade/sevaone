-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION audit.sp_insert_audit_log(
    p_id uuid, p_citizen_id uuid, p_action text, p_resource text, p_detail text,
    p_ip_address text, p_at timestamptz
)
RETURNS TABLE (success boolean, message text)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO audit.audit_logs (id, citizen_id, action, resource, detail, ip_address, at)
    VALUES (p_id, p_citizen_id, p_action, p_resource, p_detail, p_ip_address, p_at);
    RETURN QUERY SELECT TRUE, 'Audit entry recorded.'::text;
EXCEPTION WHEN OTHERS THEN
    -- Deliberately does NOT insert into audit.db_errors here: if the audit_logs insert itself
    -- is what's failing, recursively writing to another audit table on the same connection
    -- inside this EXCEPTION block risks masking the original error with a second failure.
    -- The RAISE below is enough for ExceptionHandlingMiddleware to see and log it upstream.
    RAISE;
END;
$$;
