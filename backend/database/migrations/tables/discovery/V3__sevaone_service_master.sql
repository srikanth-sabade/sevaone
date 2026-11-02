CREATE TABLE discovery.sevaone_service_master (
    id uuid NOT NULL,
    service_id character varying(32) NOT NULL,
    department_id uuid NOT NULL,
    service_name character varying(300) NOT NULL,
    category text NOT NULL,
    sakala_days integer NOT NULL,
    description text NOT NULL,
    fee text NOT NULL,
    documents_json text NOT NULL DEFAULT '[]',
    eligibility_json text NOT NULL DEFAULT '[]',
    aliases_json text NOT NULL DEFAULT '[]',
    steps_json text NOT NULL DEFAULT '[]',
    portal text,
    service_url character varying(500),
    apply_service_url character varying(500),
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_sevaone_service_master PRIMARY KEY (id),
    CONSTRAINT fk_sevaone_service_master_department FOREIGN KEY (department_id) REFERENCES discovery.department_master (id) ON DELETE RESTRICT
);
CREATE UNIQUE INDEX ix_sevaone_service_master_department_id_service_code ON discovery.sevaone_service_master (department_id, service_id);
