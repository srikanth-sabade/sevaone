CREATE TABLE orchestration.department_sessions (
    id uuid NOT NULL,
    citizen_id uuid NOT NULL,
    department_id uuid NOT NULL,
    encrypted_payload text NOT NULL,
    issued_at timestamp with time zone NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_department_sessions PRIMARY KEY (id),
    CONSTRAINT fk_department_sessions_citizen FOREIGN KEY (citizen_id) REFERENCES portal.sevaone_user (id) ON DELETE RESTRICT,
    CONSTRAINT fk_department_sessions_department FOREIGN KEY (department_id) REFERENCES discovery.department_master (id) ON DELETE RESTRICT
);
CREATE INDEX ix_department_sessions_citizen_department_expires ON orchestration.department_sessions (citizen_id, department_id, expires_at);
CREATE INDEX ix_department_sessions_department_id ON orchestration.department_sessions (department_id);
