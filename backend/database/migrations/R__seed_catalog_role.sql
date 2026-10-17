-- R__seed_catalog_role.sql
--
-- Repeatable Flyway migration: the starting set of catalog visibility audiences.
-- Idempotent via ON CONFLICT DO NOTHING against the natural business key (role_key), not
-- the surrogate uuid. Add more roles here later (e.g. a department-specific reviewer role)
-- rather than a schema migration -- that's the whole point of catalog_role being a table.

INSERT INTO discovery.catalog_role (id, role_key, display_name, description)
VALUES
    (gen_random_uuid(), 'citizen', 'Citizen', 'The public-facing Service Catalogue at /catalog.'),
    (gen_random_uuid(), 'operator', 'Operator', 'Assisted-mode counters (Karnataka One / Bangalore One).'),
    (gen_random_uuid(), 'admin', 'Admin', 'Catalog administrators -- ingestion, visibility management, audit.')
ON CONFLICT (role_key) DO NOTHING;
