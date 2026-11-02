-- V25 dropped department_url (added in V22), on the grounds that nothing read it and it was
-- redundant with portal_domain. That's no longer true: the Service Catalogue's department page
-- ("visit department portal" link) reads it directly (sp_upsert_department,
-- sp_list_departments_for_role, sp_search_departments all reference it), populated from the
-- ingested xlsx data's own per-department homepage URL -- a different value from
-- portal_domain's apply-handoff destination, same distinction V22's original comment drew.
ALTER TABLE discovery.department_master ADD COLUMN department_url text;
