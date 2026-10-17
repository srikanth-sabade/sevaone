-- EDCS onboarding/process tracking for a department -- deliberately not columns on
-- department_master: different consumer (ops staff, not the citizen-facing catalog reads),
-- different update cadence, no reason for every catalog read to carry it.
CREATE TABLE discovery.department_onboarding (
    id uuid NOT NULL,
    department_id uuid NOT NULL,
    contact_person_name character varying(200),
    designation character varying(200),
    contact_number character varying(15),
    email character varying(200),
    cc_emails text,
    nodal_officer_nomination_requested_at timestamp with time zone,
    api_doc_v1_shared_at timestamp with time zone,
    api_doc_v2_shared_at timestamp with time zone,
    ai_enabled boolean NOT NULL DEFAULT FALSE,
    redirection_enabled boolean NOT NULL DEFAULT FALSE,
    api_implemented boolean NOT NULL DEFAULT FALSE,
    updated_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_department_onboarding PRIMARY KEY (id),
    CONSTRAINT fk_department_onboarding_department FOREIGN KEY (department_id) REFERENCES discovery.department_master (id) ON DELETE RESTRICT
);
CREATE UNIQUE INDEX ix_department_onboarding_department_id ON discovery.department_onboarding (department_id);
