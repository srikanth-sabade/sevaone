-- Collapse sub_department_master into department_master via self-reference. Table was
-- referenced by exactly one function nothing in the app ever calls, and had zero live rows --
-- a clean cutover, not a data migration. hierarchy_level distinguishes a main department (1)
-- from a sub-department (2); parent_department_id is set only for level-2 rows. A sub-department
-- shares its parent's department_id-code namespace: give it its own unique code, same as any
-- top-level department.
ALTER TABLE discovery.department_master ADD COLUMN hierarchy_level smallint NOT NULL DEFAULT 1;
ALTER TABLE discovery.department_master ADD CONSTRAINT ck_department_master_hierarchy_level
    CHECK (hierarchy_level IN (1, 2));
ALTER TABLE discovery.department_master ADD COLUMN parent_department_id uuid;
ALTER TABLE discovery.department_master ADD CONSTRAINT fk_department_master_parent
    FOREIGN KEY (parent_department_id) REFERENCES discovery.department_master (id) ON DELETE RESTRICT;
ALTER TABLE discovery.department_master ADD CONSTRAINT ck_department_master_parent_matches_level
    CHECK ((hierarchy_level = 1 AND parent_department_id IS NULL)
        OR (hierarchy_level = 2 AND parent_department_id IS NOT NULL));

-- Repoint the sub-department FK on sevaone_service_master now that sub-departments live in
-- department_master too.
ALTER TABLE discovery.sevaone_service_master DROP CONSTRAINT fk_sevaone_service_master_sub_department;
ALTER TABLE discovery.sevaone_service_master ADD CONSTRAINT fk_sevaone_service_master_sub_department
    FOREIGN KEY (sub_department_id) REFERENCES discovery.department_master (id) ON DELETE SET NULL;

DROP FUNCTION IF EXISTS discovery.sp_list_sub_departments_by_department(uuid);
DROP TABLE discovery.sub_department_master;
