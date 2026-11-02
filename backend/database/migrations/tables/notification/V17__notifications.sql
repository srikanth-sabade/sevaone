CREATE TABLE notification.notifications (
    id uuid NOT NULL,
    citizen_id uuid,
    channel text NOT NULL DEFAULT 'log',
    recipient text NOT NULL,
    subject text NOT NULL,
    body text NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    sent_at timestamp with time zone,
    CONSTRAINT pk_notifications PRIMARY KEY (id)
);
