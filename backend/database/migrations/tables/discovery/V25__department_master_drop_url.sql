-- department_url (V22) turned out redundant with portal_domain: neither actually drives the
-- apply redirect (that's sevaone_service_master.apply_service_url, per-service), both are just
-- descriptive metadata on the department, and nothing in the app ever read department_url --
-- decided unnecessary right after adding it.
ALTER TABLE discovery.department_master DROP COLUMN department_url;
