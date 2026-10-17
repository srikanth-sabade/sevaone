CREATE TABLE orchestration.applications (
    id uuid NOT NULL,
    seva_reference character varying(32) NOT NULL,
    department_reference text,
    citizen_id uuid NOT NULL,
    scheme_id uuid NOT NULL,
    department_id uuid NOT NULL,
    status integer NOT NULL,
    submitted_at timestamp with time zone NOT NULL,
    sakala_due_at timestamp with time zone NOT NULL,
    applicant_name text NOT NULL,
    address text NOT NULL,
    purpose text,
    district text,
    taluk text,
    CONSTRAINT pk_applications PRIMARY KEY (id),
    CONSTRAINT fk_applications_citizen FOREIGN KEY (citizen_id) REFERENCES portal.sevaone_user (id) ON DELETE RESTRICT,
    CONSTRAINT fk_applications_department FOREIGN KEY (department_id) REFERENCES discovery.department_master (id) ON DELETE RESTRICT,
    CONSTRAINT fk_applications_scheme FOREIGN KEY (scheme_id) REFERENCES discovery.sevaone_service_master (id) ON DELETE RESTRICT
);
CREATE INDEX ix_applications_citizen_id ON orchestration.applications (citizen_id);
CREATE INDEX ix_applications_department_id ON orchestration.applications (department_id);
CREATE INDEX ix_applications_department_reference ON orchestration.applications (department_reference);
CREATE INDEX ix_applications_scheme_id ON orchestration.applications (scheme_id);
CREATE UNIQUE INDEX ix_applications_seva_reference ON orchestration.applications (seva_reference);
-- Backstop for PortalDeptFacingService.CompleteApplicationAsync's check-then-act duplicate
-- check. department_reference is part of the key so a legitimate later reapplication under a
-- new reference is never blocked -- Postgres treats each NULL department_reference as distinct.
CREATE UNIQUE INDEX ix_applications_citizen_scheme_department_ref ON orchestration.applications (citizen_id, scheme_id, department_id, department_reference);
