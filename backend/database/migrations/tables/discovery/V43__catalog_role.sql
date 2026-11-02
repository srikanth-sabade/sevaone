-- The set of audiences a catalog service can be made visible to. Deliberately a table, not
-- a fixed enum/CHECK constraint -- adding a new audience later (e.g. a department-specific
-- reviewer role) means inserting a row here, not a schema migration. Seeded via
-- R__seed_catalog_role.sql with citizen/operator/admin.
CREATE TABLE discovery.catalog_role (
    id uuid NOT NULL,
    role_key character varying(64) NOT NULL,
    display_name character varying(200) NOT NULL,
    description text,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_catalog_role PRIMARY KEY (id)
);
CREATE UNIQUE INDEX ix_catalog_role_key ON discovery.catalog_role (role_key);
