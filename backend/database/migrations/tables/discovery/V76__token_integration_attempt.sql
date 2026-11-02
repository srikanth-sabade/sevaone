-- One row per time a citizen is sent to a department through the token integration (Lane 2).
-- edcs_application_ref_no is the unique application reference the contract requires; it is what
-- the department sends back to our acknowledgement API, so everything later hangs off it.
-- The acknowledgement columns stay empty until the department calls that API.
-- The token itself is never stored here (it lives, hashed, in portal.sevaone_token via token_id).

CREATE SEQUENCE discovery.token_integration_ref_seq START WITH 100100000001;

CREATE TABLE discovery.token_integration_attempt (
    id uuid NOT NULL,
    edcs_application_ref_no varchar(32) NOT NULL,
    service_id uuid NOT NULL,
    department_id uuid NOT NULL,
    citizen_id uuid NOT NULL,
    sevaone_user_id bigint NOT NULL,
    token_id uuid,
    assisted_mode boolean NOT NULL DEFAULT false,
    status varchar(24) NOT NULL DEFAULT 'initiated',
    landing_url text NOT NULL,
    sent_fields jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    expires_at timestamp with time zone NOT NULL,
    department_application_ref_no varchar(100),
    department_payment_status varchar(50),
    department_fee numeric(12,2),
    department_fee_collection_date timestamp with time zone,
    department_payment_ref_number varchar(100),
    acknowledgment_file_type varchar(20),
    acknowledgment_date timestamp with time zone,
    acknowledgment bytea,
    acknowledged_at timestamp with time zone,
    last_payload jsonb,
    CONSTRAINT pk_token_integration_attempt PRIMARY KEY (id),
    CONSTRAINT uq_token_integration_attempt_ref UNIQUE (edcs_application_ref_no),
    CONSTRAINT ck_token_integration_attempt_status CHECK (status IN ('initiated', 'acknowledged', 'failed')),
    CONSTRAINT fk_token_integration_attempt_service FOREIGN KEY (service_id) REFERENCES discovery.sevaone_service_master (id),
    CONSTRAINT fk_token_integration_attempt_department FOREIGN KEY (department_id) REFERENCES discovery.department_master (id),
    CONSTRAINT fk_token_integration_attempt_citizen FOREIGN KEY (citizen_id) REFERENCES portal.sevaone_user (id),
    CONSTRAINT fk_token_integration_attempt_token FOREIGN KEY (token_id) REFERENCES portal.sevaone_token (id) ON DELETE SET NULL
);
CREATE INDEX ix_token_integration_attempt_citizen ON discovery.token_integration_attempt (citizen_id, created_at DESC);
CREATE INDEX ix_token_integration_attempt_service ON discovery.token_integration_attempt (service_id, created_at DESC);
