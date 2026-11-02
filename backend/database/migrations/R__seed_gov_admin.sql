-- R__seed_gov_admin.sql
--
-- Repeatable Flyway migration: one seed gov-admin account per tier, for local dev/testing.
-- Idempotent via ON CONFLICT DO NOTHING against the natural business key (username), not the
-- surrogate uuid -- username is stable from creation, unlike wso2_subject (NULL until claimed).
--
-- These 5 rows are pre-claimed (wso2_subject already set = username) so existing local dev/test
-- setups keep working without re-running the claim flow -- see V69__gov_admin_master_claim_flow.sql
-- for why wso2_subject is nullable at all. A REAL Superadmin-created account has wso2_subject
-- NULL until the new admin completes GovAdminClaimService's mobile-OTP + password claim.
--
-- PREREQUISITE (manual, out of band, NOT done by this migration): a matching WSO2 IS SCIM2 user
-- for each username below must already exist and be a member of the SevaOne_GovAdmin group (see
-- Docker-deploy/wso2/provision.sh) before login will actually work -- this migration only
-- creates the LOCAL RBAC row. Gov admins have no local password -- WSO2 IS is the only
-- credential store for this persona.

INSERT INTO portal.gov_admin_master (id, username, wso2_subject, display_name, tier, assigned_district, assigned_taluk, assigned_department, is_active, created_at)
VALUES
    ('b1c2d3e4-f5a6-4789-9abc-def012340001', 'superadmin', 'superadmin', 'Seed Superadmin', 'Superadmin', NULL, NULL, NULL, TRUE, now()),
    ('b1c2d3e4-f5a6-4789-9abc-def012340002', 'chiefminister', 'chiefminister', 'Seed Chief Minister', 'ChiefMinister', NULL, NULL, NULL, TRUE, now()),
    ('b1c2d3e4-f5a6-4789-9abc-def012340003', 'official', 'official', 'Seed Department Admin (Health)', 'Department', NULL, NULL, 'health', TRUE, now()),
    ('b1c2d3e4-f5a6-4789-9abc-def012340004', 'district', 'district', 'Seed District Head (Kalaburagi)', 'DistrictHead', 'Kalaburagi', NULL, NULL, TRUE, now()),
    ('b1c2d3e4-f5a6-4789-9abc-def012340005', 'taluk', 'taluk', 'Seed Taluk Head (Kalaburagi / Jevargi)', 'TalukHead', 'Kalaburagi', 'Jevargi', NULL, TRUE, now())
ON CONFLICT (username) DO NOTHING;
