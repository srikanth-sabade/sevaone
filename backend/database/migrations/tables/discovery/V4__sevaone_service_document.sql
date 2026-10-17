-- No direct department_id/service_id columns -- reachable via scheme_id -> sevaone_service_master.
-- See discovery.v_sevaone_service_document_flat (migrations/views) for a flat/denormalized read.
CREATE TABLE discovery.sevaone_service_document (
    id uuid NOT NULL,
    scheme_id uuid NOT NULL,
    document_name character varying(200),
    document_url character varying(500) NOT NULL,
    document_type character varying(50),
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_sevaone_service_document PRIMARY KEY (id),
    CONSTRAINT fk_sevaone_service_document_scheme FOREIGN KEY (scheme_id) REFERENCES discovery.sevaone_service_master (id) ON DELETE RESTRICT
);
CREATE INDEX ix_sevaone_service_document_scheme_id ON discovery.sevaone_service_document (scheme_id);
