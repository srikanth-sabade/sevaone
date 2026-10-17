-- Fields from the EDCS master service-catalog sheet that sevaone_service_master didn't carry
-- yet. sub_department_id is nullable -- not every department has sub-departments.
ALTER TABLE discovery.sevaone_service_master ADD COLUMN sub_department_id uuid;
ALTER TABLE discovery.sevaone_service_master ADD CONSTRAINT fk_sevaone_service_master_sub_department
    FOREIGN KEY (sub_department_id) REFERENCES discovery.sub_department_master (id) ON DELETE SET NULL;

ALTER TABLE discovery.sevaone_service_master ADD COLUMN service_name_kannada character varying(300);
ALTER TABLE discovery.sevaone_service_master ADD COLUMN short_description text;
-- The department's own internal code for this service -- separate from service_id, which is
-- SevaOne's own business code (SVC001-style). Two different identifiers for the same service.
ALTER TABLE discovery.sevaone_service_master ADD COLUMN department_service_id character varying(64);
ALTER TABLE discovery.sevaone_service_master ADD COLUMN jurisdiction character varying(100);

ALTER TABLE discovery.sevaone_service_master ADD COLUMN mode_of_service character varying(20);
ALTER TABLE discovery.sevaone_service_master ADD CONSTRAINT ck_sevaone_service_master_mode_of_service
    CHECK (mode_of_service IS NULL OR mode_of_service IN ('Online', 'Offline', 'Both'));

ALTER TABLE discovery.sevaone_service_master ADD COLUMN is_mandatory boolean;
ALTER TABLE discovery.sevaone_service_master ADD COLUMN remarks text;

-- The real numeric fee breakdown the catalog data actually carries (department's own statutory
-- fee vs. SevaOne's service charge). The existing free-text `fee` column stays as-is for
-- backward compatibility with the current application code -- not populated by new imports.
ALTER TABLE discovery.sevaone_service_master ADD COLUMN department_fee numeric(12,2);
ALTER TABLE discovery.sevaone_service_master ADD COLUMN service_charge numeric(12,2);
