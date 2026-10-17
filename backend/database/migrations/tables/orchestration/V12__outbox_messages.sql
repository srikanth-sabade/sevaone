CREATE TABLE orchestration.outbox_messages (
    id uuid NOT NULL,
    topic text NOT NULL,
    payload text NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    published_at timestamp with time zone,
    attempts integer NOT NULL DEFAULT 0,
    last_error text,
    CONSTRAINT pk_outbox_messages PRIMARY KEY (id)
);
CREATE INDEX ix_outbox_messages_published_at ON orchestration.outbox_messages (published_at);
