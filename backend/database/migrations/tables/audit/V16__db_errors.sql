-- Written by every sp_* write function's EXCEPTION handler (all schemas) -- see
-- backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md. Observability only, not the only failure
-- signal the app sees -- each function logs here AND re-raises.
CREATE TABLE audit.db_errors (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    proc_name text NOT NULL,
    sql_state text,
    error_message text NOT NULL,
    occurred_at timestamp with time zone NOT NULL DEFAULT now()
);
CREATE INDEX ix_db_errors_proc_name_occurred_at ON audit.db_errors (proc_name, occurred_at DESC);
