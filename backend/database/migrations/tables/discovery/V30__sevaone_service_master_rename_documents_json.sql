-- "documents_json" was ambiguous next to sevaone_service_document (the real per-document table) --
-- this is specifically the required-documents list for the service, so name it that.
ALTER TABLE discovery.sevaone_service_master RENAME COLUMN documents_json TO required_documents_json;
