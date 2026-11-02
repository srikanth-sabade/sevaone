CREATE TABLE IF NOT EXISTS portal.gov_notifications
(
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    title       varchar(160) NOT NULL,
    body        text NOT NULL,
    is_active   boolean NOT NULL DEFAULT true,
    created_by  uuid NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_gov_notifications_active_created
    ON portal.gov_notifications (is_active, created_at DESC);
