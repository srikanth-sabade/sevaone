-- One row per catalog file/JSON upload. An upload never touches department_master/
-- sub_department_master/sevaone_service_master directly -- it lands as catalog_import_row
-- rows an admin reviews, and only publish (status -> 'published') writes the live tables.
CREATE TABLE discovery.catalog_import_batch (
    id uuid NOT NULL,
    source_filename character varying(500) NOT NULL,
    source_format character varying(16) NOT NULL,
    uploaded_by text NOT NULL,
    uploaded_at timestamp with time zone NOT NULL DEFAULT now(),
    status character varying(32) NOT NULL DEFAULT 'validating',
    row_count integer NOT NULL DEFAULT 0,
    error_count integer NOT NULL DEFAULT 0,
    warning_count integer NOT NULL DEFAULT 0,
    published_at timestamp with time zone,
    published_by text,
    CONSTRAINT pk_catalog_import_batch PRIMARY KEY (id),
    CONSTRAINT ck_catalog_import_batch_source_format CHECK (source_format IN ('xlsx', 'csv', 'json')),
    CONSTRAINT ck_catalog_import_batch_status CHECK (status IN ('validating', 'ready_to_publish', 'published', 'failed', 'discarded'))
);
CREATE INDEX ix_catalog_import_batch_uploaded_at ON discovery.catalog_import_batch (uploaded_at DESC);
