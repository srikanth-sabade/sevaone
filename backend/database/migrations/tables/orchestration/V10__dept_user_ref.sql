CREATE TABLE orchestration.dept_user_ref (
    id uuid NOT NULL,
    department_id uuid NOT NULL,
    citizen_id uuid NOT NULL,
    operator_id uuid,
    assisted_mode boolean NOT NULL DEFAULT FALSE,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_dept_user_ref PRIMARY KEY (id),
    CONSTRAINT fk_dept_user_ref_department FOREIGN KEY (department_id) REFERENCES discovery.department_master (id) ON DELETE RESTRICT,
    CONSTRAINT fk_dept_user_ref_operator FOREIGN KEY (operator_id) REFERENCES portal.operator_master (id) ON DELETE RESTRICT,
    CONSTRAINT fk_dept_user_ref_citizen FOREIGN KEY (citizen_id) REFERENCES portal.sevaone_user (id) ON DELETE RESTRICT
);
CREATE INDEX ix_dept_user_ref_citizen_id ON orchestration.dept_user_ref (citizen_id);
CREATE UNIQUE INDEX ix_dept_user_ref_department_id_citizen_id ON orchestration.dept_user_ref (department_id, citizen_id);
CREATE INDEX ix_dept_user_ref_operator_id ON orchestration.dept_user_ref (operator_id);
