-- Same pattern as sevaone_service_master.service_name_kannada -- citizen-facing English text
-- that had no Kannada counterpart anywhere else in the catalog. DB-only for now, matching how
-- service_name_kannada itself was added: nothing in this codebase writes to department_master
-- or sevaone_service_master today (no seed/import populates *_kannada yet either), so there's
-- no function or C# wiring to add until a real EDCS Kannada import exists.
ALTER TABLE discovery.department_master ADD COLUMN department_name_kannada character varying(200);
ALTER TABLE discovery.department_master ADD COLUMN portal_name_kannada text;

ALTER TABLE discovery.sevaone_service_master ADD COLUMN description_kannada text;
ALTER TABLE discovery.sevaone_service_master ADD COLUMN short_description_kannada text;

ALTER TABLE discovery.sub_department_master ADD COLUMN sub_department_name_kannada character varying(200);
