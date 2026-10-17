-- Which roles can see which service -- a fact about the data, not a filter applied later.
-- A service with no row here for a given role is genuinely absent from that role's query
-- results (404, not a filtered-out 200). Pure join table, so the composite natural key
-- doubles as the PK -- no surrogate id needed, unlike the entity tables in this schema.
CREATE TABLE discovery.service_role_visibility (
    service_id uuid NOT NULL,
    role_id uuid NOT NULL,
    granted_at timestamp with time zone NOT NULL DEFAULT now(),
    granted_by text,
    CONSTRAINT pk_service_role_visibility PRIMARY KEY (service_id, role_id),
    CONSTRAINT fk_service_role_visibility_service FOREIGN KEY (service_id) REFERENCES discovery.sevaone_service_master (id) ON DELETE CASCADE,
    CONSTRAINT fk_service_role_visibility_role FOREIGN KEY (role_id) REFERENCES discovery.catalog_role (id) ON DELETE CASCADE
);
CREATE INDEX ix_service_role_visibility_role_id ON discovery.service_role_visibility (role_id);
