-- Dedicated gov-admin activity log -- NOT a retrofit of audit.audit_logs, which is citizen-scoped
-- (a `citizen_id` column, no admin-actor column) and whose only read path filters by citizen id.
-- Admin-originated rows there (gov_admin.login/.logout, CitizenId = null) were already
-- unreadable dead data before this migration; this table is a real, queryable replacement,
-- mirroring discovery.service_change_log's append-only shape.
CREATE TABLE portal.gov_admin_activity_log (
    id uuid NOT NULL,
    actor_username character varying(100) NOT NULL,
    actor_display_name character varying(200) NOT NULL,
    action character varying(64) NOT NULL,
    target text,
    detail text,
    at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_gov_admin_activity_log PRIMARY KEY (id)
);
CREATE INDEX ix_gov_admin_activity_log_at ON portal.gov_admin_activity_log (at DESC);
CREATE INDEX ix_gov_admin_activity_log_action ON portal.gov_admin_activity_log (action, at DESC);
