-- Remaining gaps found while wiring up the Service Catalogue ingestion pipeline (the
-- xlsx/EDCS-shaped catalog sheets carry an explicit Sakala enabled/disabled flag distinct
-- from sakala_days, and Kannada eligibility/procedure text that documents_json's sibling
-- columns never got a language split for -- documents themselves already moved to the
-- per-row sevaone_service_document table with Kannada support, see V24).
ALTER TABLE discovery.sevaone_service_master ADD COLUMN is_sakala_enabled boolean;
-- Preserves the source's raw text ("Non-Sakala", "non sakala", "Yes", ...) for audit even
-- after is_sakala_enabled normalizes it to a boolean -- source data was inconsistently cased.
ALTER TABLE discovery.sevaone_service_master ADD COLUMN sakala_label_raw text;
ALTER TABLE discovery.sevaone_service_master ADD COLUMN eligibility_json_kannada text NOT NULL DEFAULT '[]';
ALTER TABLE discovery.sevaone_service_master ADD COLUMN steps_json_kannada text NOT NULL DEFAULT '[]';
-- Bumped on every publish that changes this row -- pairs with discovery.service_change_log.
ALTER TABLE discovery.sevaone_service_master ADD COLUMN version integer NOT NULL DEFAULT 1;
