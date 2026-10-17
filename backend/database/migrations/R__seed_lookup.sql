-- R__seed_lookup.sql
--
-- Repeatable Flyway migration: reference/lookup catalog data (departments only -- the
-- ~40-row placeholder service seed this file used to carry was superseded by the real
-- priority_10_departments.xlsx import (765 services across these same 10 departments, done
-- via the Catalog Admin import/publish pipeline -- see CatalogIngestionService) and is no
-- longer seeded here, so a fresh environment doesn't need it deactivated after the fact the
-- way an already-migrated one was (V58). Only the 10 departments the xlsx actually covers are
-- seeded; urban/home/commercial-taxes never appeared in that xlsx and are dropped rather than
-- deactivated after the fact, so a fresh environment matches an already-migrated one without
-- depending on V58's cleanup UPDATE. Re-run whenever this file changes; idempotent via
-- ON CONFLICT DO NOTHING against the natural business key (department_id), not the surrogate
-- uuid.

-- Revenue once had a real connector (connector_type = 'revenue', the Nadakacheri mock) but that
-- mock backend has since been deleted -- there is currently nothing on the other end for any
-- department, Revenue included, that would ever redeem a citizen-minted JWS token. All 9 rows
-- below are is_integrated = FALSE for that reason (see V63/V64 for the migrations that
-- corrected this same mistake on already-seeded environments, and why it was wrong: a stale
-- TRUE here forces anonymous citizens through a needless login wall for services that are
-- really just plain department website links). connector_type stays 'revenue' as a historical
-- marker; it is not read anywhere in application code. This is independent of the real, live
-- operator-assisted integrations several departments have via scheme.integration_method
-- ('ajsk'/'sakala_token'/'sakala_token_escom') -- those are gated separately in
-- ApplyRouteClassifier and unaffected by this flag.
INSERT INTO discovery.department_master
    (id, department_id, department_name, portal_name, portal_domain, connector_type, is_integrated, is_active)
VALUES
    (gen_random_uuid(), 'revenue', 'Revenue Department', 'Nadakacheri / Atalji Janasnehi Directorate', 'nadakacheri.karnataka.gov.in', 'revenue', FALSE, TRUE),
    (gen_random_uuid(), 'transport', 'Transport Department', 'Sarathi / Vahan Karnataka', 'transport.karnataka.gov.in', 'generic', FALSE, TRUE),
    (gen_random_uuid(), 'health', 'Health & Family Welfare', 'Arogya Karnataka', 'arogya.karnataka.gov.in', 'generic', FALSE, TRUE),
    (gen_random_uuid(), 'food', 'Food and Civil Supplies', 'Ahara Karnataka', 'ahara.kar.nic.in', 'generic', FALSE, TRUE),
    (gen_random_uuid(), 'labour', 'Labour Department', 'Karmika Spandana', 'labour.karnataka.gov.in', 'generic', FALSE, TRUE),
    (gen_random_uuid(), 'wcd', 'Women & Child Development', 'Mahiti Kanaja WCD', 'dwcd.karnataka.gov.in', 'generic', FALSE, TRUE),
    (gen_random_uuid(), 'social-welfare', 'Social Welfare', 'Samaj Kalyana e-Seva', 'sw.kar.nic.in', 'generic', FALSE, TRUE),
    (gen_random_uuid(), 'education', 'Education Department', 'SATS / School Education Portal', 'schooleducation.kar.nic.in', 'generic', FALSE, TRUE),
    (gen_random_uuid(), 'rdpr', 'Rural Development & Panchayat Raj', 'Panchatantra / RDPR e-Seva', 'rdpr.karnataka.gov.in', 'generic', FALSE, TRUE)
ON CONFLICT (department_id) DO NOTHING;
