-- One row per apply attempt (real deep-link token mint OR homepage-fallback redirect -- both
-- represent a citizen genuinely trying to apply, so both count). testMode mints (department
-- integration testing via /dept/test-token) are deliberately never logged here -- synthetic QA
-- traffic shouldn't pollute real demand data. Log table, not a bare counter column on
-- sevaone_service_master, so the count is always reconstructable from what actually happened
-- (and the same row set supports "count" today and "trend over time" / "by department" later
-- without a schema change) -- same convention as discovery.service_change_log.
CREATE TABLE discovery.service_apply_track (
    id uuid NOT NULL,
    scheme_id uuid,
    department_id uuid,
    citizen_id uuid,
    assisted_mode boolean NOT NULL DEFAULT false,
    is_homepage_fallback boolean NOT NULL DEFAULT false,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_service_apply_track PRIMARY KEY (id),
    CONSTRAINT fk_service_apply_track_scheme FOREIGN KEY (scheme_id) REFERENCES discovery.sevaone_service_master (id) ON DELETE SET NULL,
    CONSTRAINT fk_service_apply_track_department FOREIGN KEY (department_id) REFERENCES discovery.department_master (id) ON DELETE SET NULL,
    CONSTRAINT fk_service_apply_track_citizen FOREIGN KEY (citizen_id) REFERENCES portal.sevaone_user (id) ON DELETE SET NULL
);
CREATE INDEX ix_service_apply_track_scheme_id ON discovery.service_apply_track (scheme_id, created_at DESC);
CREATE INDEX ix_service_apply_track_created_at ON discovery.service_apply_track (created_at DESC);
