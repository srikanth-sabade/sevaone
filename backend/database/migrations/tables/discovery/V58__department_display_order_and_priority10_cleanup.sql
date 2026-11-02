-- The Service Catalogue (Catalog.tsx / sp_list_departments_for_role) is now backed by the
-- 10-department "priority_10_departments.xlsx" import (765 real services). department_master
-- has no ordering column, so it fell back to alphabetical -- this adds one so the catalogue
-- can present the same department order as the xlsx's own Index sheet, which is not
-- alphabetical (Revenue, Transport, Education, Labour, Social Welfare, RDPR, Agriculture,
-- Food, Health, WCD). Sub-department order needs no equivalent change: the Index sheet lists
-- each department's sub-departments alphabetically already, matching
-- sp_list_sub_departments_for_role's existing "ORDER BY display_order, sub_department_name"
-- fallback (display_order is admin-curated and 0 for every ingested row).
ALTER TABLE discovery.department_master ADD COLUMN display_order integer NOT NULL DEFAULT 0;

UPDATE discovery.department_master SET display_order = ordering.seq
FROM (VALUES
    ('revenue', 1),
    ('transport', 2),
    ('education', 3),
    ('labour', 4),
    ('social-welfare', 5),
    ('rdpr', 6),
    ('agriculture', 7),
    ('food', 8),
    ('health', 9),
    ('wcd', 10)
) AS ordering(department_id, seq)
WHERE discovery.department_master.department_id = ordering.department_id;

-- These 3 were never part of the xlsx's 10 departments -- deactivated (not deleted) so a
-- reset/reseed of R__seed_lookup.sql doesn't need to change shape, and so the change is
-- trivially reversible. is_active = FALSE already excludes them from every catalogue listing
-- function (sp_list_departments_for_role, sp_list_departments_with_scheme_count).
UPDATE discovery.department_master SET is_active = FALSE
WHERE department_id IN ('commercial-taxes', 'home', 'urban');

-- The ~40 SVC### placeholder services from R__seed_lookup.sql predate the real xlsx import
-- and never had a service_role_visibility grant (confirmed: 0 grants for all 40), so they
-- were never visible on /catalog -- this just stops them lingering as dead rows under
-- departments that now hold real data. Deactivated, not deleted, for the same reversibility
-- reason as above.
UPDATE discovery.sevaone_service_master SET is_active = FALSE
WHERE service_id LIKE 'SVC%';
