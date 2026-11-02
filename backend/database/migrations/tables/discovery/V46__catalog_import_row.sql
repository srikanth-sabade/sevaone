-- One row per service parsed out of a catalog_import_batch. Staging only -- never read by
-- the live citizen/operator catalog, purely the admin review surface before publish.
-- raw_payload/normalized_payload/validation_errors/validation_warnings are JSON, stored as
-- text like every other JSON column in this schema (documents_json, eligibility_json, ...)
-- rather than jsonb, so no new Dapper type handling is needed to read them.
CREATE TABLE discovery.catalog_import_row (
    id uuid NOT NULL,
    batch_id uuid NOT NULL,
    row_number integer NOT NULL,
    raw_payload text NOT NULL,
    normalized_payload text,
    action character varying(16),
    validation_errors text NOT NULL DEFAULT '[]',
    validation_warnings text NOT NULL DEFAULT '[]',
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_catalog_import_row PRIMARY KEY (id),
    CONSTRAINT fk_catalog_import_row_batch FOREIGN KEY (batch_id) REFERENCES discovery.catalog_import_batch (id) ON DELETE CASCADE,
    CONSTRAINT ck_catalog_import_row_action CHECK (action IS NULL OR action IN ('insert', 'update', 'unchanged', 'conflict'))
);
CREATE INDEX ix_catalog_import_row_batch_id ON discovery.catalog_import_row (batch_id, row_number);
