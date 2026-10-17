-- Append-only audit trail: who changed what on which service, when, and (if it came from an
-- upload) which batch. Independent of catalog_import_row -- manual single-service edits and
-- visibility grants/revokes log here too, not just imports. service_id/batch_id are nullable
-- and ON DELETE SET NULL so the log survives a service or batch being removed later.
CREATE TABLE discovery.service_change_log (
    id uuid NOT NULL,
    service_id uuid,
    batch_id uuid,
    changed_at timestamp with time zone NOT NULL DEFAULT now(),
    changed_by text NOT NULL,
    change_type character varying(32) NOT NULL,
    diff text,
    CONSTRAINT pk_service_change_log PRIMARY KEY (id),
    CONSTRAINT fk_service_change_log_service FOREIGN KEY (service_id) REFERENCES discovery.sevaone_service_master (id) ON DELETE SET NULL,
    CONSTRAINT fk_service_change_log_batch FOREIGN KEY (batch_id) REFERENCES discovery.catalog_import_batch (id) ON DELETE SET NULL,
    CONSTRAINT ck_service_change_log_change_type CHECK (change_type IN ('created', 'updated', 'deactivated', 'reactivated', 'role_grant', 'role_revoke'))
);
CREATE INDEX ix_service_change_log_service_id ON discovery.service_change_log (service_id, changed_at DESC);
CREATE INDEX ix_service_change_log_changed_at ON discovery.service_change_log (changed_at DESC);
