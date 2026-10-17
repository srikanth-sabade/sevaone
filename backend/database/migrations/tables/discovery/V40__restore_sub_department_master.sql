-- V32 dropped sub_department_master, folding sub-departments into department_master via
-- hierarchy_level/parent_department_id, on the grounds that the table was unused and had zero
-- live rows at the time. That's no longer true: the Service Catalogue's real, migrated data
-- populates it with ~90 real sub-departments (e.g. Education has 12, Revenue has 5), and a
-- full write/read path (sp_upsert_sub_department, sp_list_sub_departments_for_role, the
-- SubDepartment domain model, the admin console) was built directly against it as a
-- first-class table.
--
-- Restored to its pre-V32 shape (V20) plus the is_active/display_order columns V25 added on
-- top of that shape. department_master's hierarchy_level/parent_department_id columns are left
-- as-is -- unrelated to this table, and not used by anything in the Service Catalogue.
CREATE TABLE discovery.sub_department_master (
    id uuid NOT NULL,
    department_id uuid NOT NULL,
    sub_department_name character varying(200) NOT NULL,
    is_active boolean NOT NULL DEFAULT TRUE,
    display_order integer NOT NULL DEFAULT 0,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_sub_department_master PRIMARY KEY (id),
    CONSTRAINT fk_sub_department_master_department FOREIGN KEY (department_id) REFERENCES discovery.department_master (id) ON DELETE RESTRICT
);
CREATE UNIQUE INDEX ix_sub_department_master_department_id_name ON discovery.sub_department_master (department_id, sub_department_name);

ALTER TABLE discovery.sevaone_service_master DROP CONSTRAINT fk_sevaone_service_master_sub_department;
ALTER TABLE discovery.sevaone_service_master ADD CONSTRAINT fk_sevaone_service_master_sub_department
    FOREIGN KEY (sub_department_id) REFERENCES discovery.sub_department_master (id) ON DELETE SET NULL;
