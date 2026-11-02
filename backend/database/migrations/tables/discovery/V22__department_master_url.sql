-- The department's own public website -- distinct from portal_domain, which is where SevaOne
-- redirects citizens for the apply handoff, not necessarily the department's general site.
ALTER TABLE discovery.department_master ADD COLUMN department_url text;
