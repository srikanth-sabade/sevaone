CREATE TABLE discovery.department_master (
    id uuid NOT NULL,
    department_id character varying(64) NOT NULL,
    department_name character varying(200) NOT NULL,
    portal_name text NOT NULL,
    portal_domain text NOT NULL,
    connector_type text NOT NULL,
    is_integrated boolean NOT NULL,
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_department_master PRIMARY KEY (id)
);
CREATE UNIQUE INDEX ix_department_master_code ON discovery.department_master (department_id);
