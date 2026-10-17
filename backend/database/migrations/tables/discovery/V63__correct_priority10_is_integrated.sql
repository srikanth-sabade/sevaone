-- R__seed_lookup.sql originally hand-seeded is_integrated = TRUE for all 9 of the "priority
-- 10" departments it lists (Revenue, Transport, Education, Labour, Social Welfare, RDPR, Food,
-- Health, WCD) -- but only Revenue ever actually got a real connector built
-- (connector_type = 'revenue', the Nadakacheri integration). The other 8 were marked TRUE
-- aspirationally, since they were the departments planned for integration, not because any
-- backend work was ever done for them -- contrast discovery.sp_upsert_department, which
-- correctly defaults a brand-new department to is_integrated = FALSE ("no connector wiring
-- yet -- that's set up by a separate, human process").
--
-- Practical effect of the stale flag: ApplyRouteClassifier.Classify treats these 8 the same as
-- Revenue, so any service of theirs that happens to have a deeplink_citizen_url populated
-- (common -- the catalog import fills it from CSV data independent of this flag) forces an
-- anonymous citizen through a login wall for what is really just a plain department website
-- link, exactly the bug already fixed for Agriculture (which correctly defaulted to FALSE via
-- sp_upsert_department, never having been in R__seed_lookup.sql's hand-written list at all).
UPDATE discovery.department_master
SET is_integrated = FALSE
WHERE department_id IN ('transport', 'health', 'food', 'labour', 'wcd', 'social-welfare', 'education', 'rdpr')
  AND connector_type <> 'revenue';
