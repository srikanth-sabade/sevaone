CREATE TABLE discovery.sub_department_master (
    id uuid NOT NULL,
    department_id uuid NOT NULL,
    sub_department_name character varying(200) NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_sub_department_master PRIMARY KEY (id),
    CONSTRAINT fk_sub_department_master_department FOREIGN KEY (department_id) REFERENCES discovery.department_master (id) ON DELETE RESTRICT
);
CREATE UNIQUE INDEX ix_sub_department_master_department_id_name ON discovery.sub_department_master (department_id, sub_department_name);
