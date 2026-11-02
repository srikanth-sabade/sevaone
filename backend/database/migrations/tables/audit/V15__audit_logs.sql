CREATE TABLE audit.audit_logs (
    id uuid NOT NULL,
    citizen_id uuid,
    action text NOT NULL,
    resource text NOT NULL,
    detail text,
    ip_address text,
    at timestamp with time zone NOT NULL,
    CONSTRAINT pk_audit_logs PRIMARY KEY (id)
);
CREATE INDEX ix_audit_logs_at ON audit.audit_logs (at);
