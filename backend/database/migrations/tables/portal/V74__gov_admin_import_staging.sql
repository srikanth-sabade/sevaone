CREATE TABLE portal.gov_admin_import_batch (
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
    CONSTRAINT pk_gov_admin_import_batch PRIMARY KEY (id),
    CONSTRAINT ck_gov_admin_import_batch_source_format CHECK (source_format IN ('csv', 'xlsx')),
    CONSTRAINT ck_gov_admin_import_batch_status CHECK (status IN ('validating', 'ready_to_publish', 'published', 'failed', 'discarded'))
);

CREATE TABLE portal.gov_admin_import_row (
    id uuid NOT NULL,
    batch_id uuid NOT NULL REFERENCES portal.gov_admin_import_batch(id) ON DELETE CASCADE,
    row_number integer NOT NULL,
    raw_payload jsonb NOT NULL,
    action character varying(32),
    validation_errors jsonb NOT NULL DEFAULT '[]'::jsonb,
    validation_warnings jsonb NOT NULL DEFAULT '[]'::jsonb,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_gov_admin_import_row PRIMARY KEY (id),
    CONSTRAINT uq_gov_admin_import_row_number UNIQUE (batch_id, row_number)
);

CREATE INDEX ix_gov_admin_import_batch_uploaded_at ON portal.gov_admin_import_batch (uploaded_at DESC);
CREATE INDEX ix_gov_admin_import_row_batch ON portal.gov_admin_import_row (batch_id, row_number);
