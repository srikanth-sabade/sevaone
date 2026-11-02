CREATE TABLE status.application_events (
    id uuid NOT NULL,
    application_id uuid NOT NULL,
    status integer NOT NULL,
    note text NOT NULL,
    at timestamp with time zone NOT NULL,
    CONSTRAINT pk_application_events PRIMARY KEY (id),
    CONSTRAINT fk_application_events_application FOREIGN KEY (application_id) REFERENCES orchestration.applications (id) ON DELETE CASCADE
);
CREATE INDEX ix_application_events_application_id ON status.application_events (application_id);
