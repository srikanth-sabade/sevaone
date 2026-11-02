-- The catalog sheet's "Required Documents English/Kannada" belong here, as rows in the
-- existing per-document table, not as another documents_json blob on the service.
ALTER TABLE discovery.sevaone_service_document ADD COLUMN document_name_kannada character varying(200);
ALTER TABLE discovery.sevaone_service_document ADD COLUMN is_mandatory boolean NOT NULL DEFAULT TRUE;
