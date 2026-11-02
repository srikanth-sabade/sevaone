CREATE TABLE IF NOT EXISTS portal.service_notice_history
(
    id          uuid PRIMARY KEY,
    title       varchar(120) NOT NULL,
    message     varchar(500) NOT NULL,
    severity    varchar(16) NOT NULL CHECK (severity IN ('info', 'warning', 'critical')),
    was_active  boolean NOT NULL,
    starts_at   timestamptz NULL,
    ends_at     timestamptz NULL,
    saved_by    uuid NULL,
    saved_at    timestamptz NOT NULL,
    CONSTRAINT ck_service_notice_history_schedule
        CHECK (ends_at IS NULL OR starts_at IS NULL OR ends_at > starts_at)
);

CREATE INDEX IF NOT EXISTS ix_service_notice_history_saved_at
    ON portal.service_notice_history (saved_at DESC);
