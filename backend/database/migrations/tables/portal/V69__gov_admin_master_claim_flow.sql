-- Adds the Superadmin-driven admin-creation + first-time-login claim flow (mirrors
-- portal.operator_master's wso2_subject-claim pattern -- see V57__operator_master_wso2_subject.sql
-- and OperatorClaimService) to portal.gov_admin_master. Until now every gov_admin_master row was
-- created already-claimed (a manual seed + a manually-created WSO2 SCIM2 user); this migration
-- splits that into two steps: Superadmin creates the row (tier/district/taluk/department already
-- assigned, no WSO2 account yet), the new admin then claims it via mobile OTP + a password they
-- choose, which is when the real WSO2 IS account gets created (GovAdminClaimService).
--
-- username is the permanent, Superadmin-chosen login identifier (parallel to
-- operator_master.operator_id) -- stable from creation, unlike wso2_subject which stays NULL
-- until claimed. Once claimed, wso2_subject is set equal to username (the claim flow provisions
-- the WSO2 SCIM2 account using this exact value), so login always looks up by username and only
-- checks wso2_subject for "has this account been claimed yet".
ALTER TABLE portal.gov_admin_master ADD COLUMN username character varying(100);
UPDATE portal.gov_admin_master SET username = wso2_subject WHERE username IS NULL;
ALTER TABLE portal.gov_admin_master ALTER COLUMN username SET NOT NULL;
CREATE UNIQUE INDEX ix_gov_admin_master_username ON portal.gov_admin_master (username);

ALTER TABLE portal.gov_admin_master ALTER COLUMN wso2_subject DROP NOT NULL;
DROP INDEX portal.ix_gov_admin_master_wso2_subject;
CREATE UNIQUE INDEX ix_gov_admin_master_wso2_subject ON portal.gov_admin_master (wso2_subject) WHERE wso2_subject IS NOT NULL;

ALTER TABLE portal.gov_admin_master ADD COLUMN mobile_no character varying(15);
ALTER TABLE portal.gov_admin_master ADD COLUMN mobile_verified_at timestamp with time zone;

-- "GovtOfficial" (statewide read-only) is replaced by "Department" (scoped to one department --
-- see discovery.department_master, already the real department catalog, no new master data
-- needed). assigned_department stores that department's department_id code (e.g. "health"), not
-- a FK -- matches assigned_district/assigned_taluk's free-text-column shape, and department
-- codes ARE canonical (unlike citizen-supplied district/taluk text), so no matching-headache risk.
ALTER TABLE portal.gov_admin_master ADD COLUMN assigned_department character varying(64);

-- Constraint dropped BEFORE the backfill below (not after): the old constraint still lists
-- 'GovtOfficial', not 'Department', so setting tier='Department' while it's still active would
-- fail the same way as setting assigned_department on the old district-scope constraint would.
ALTER TABLE portal.gov_admin_master DROP CONSTRAINT ck_gov_admin_master_tier;

-- Pre-existing "GovtOfficial" rows (V68's seed) predate the tier rename below -- migrate them to
-- the new "Department" tier with a real department assignment before the new constraint (which
-- no longer allows 'GovtOfficial') gets added back. "health" is an arbitrary but real, active
-- department code (see discovery.department_master) -- a genuinely wrong department assignment
-- for a real deployment is a data-entry fix, not a migration concern.
UPDATE portal.gov_admin_master SET tier = 'Department', assigned_department = 'health' WHERE tier = 'GovtOfficial';

ALTER TABLE portal.gov_admin_master ADD CONSTRAINT ck_gov_admin_master_tier
    CHECK (tier IN ('Superadmin', 'ChiefMinister', 'Department', 'DistrictHead', 'TalukHead'));

ALTER TABLE portal.gov_admin_master DROP CONSTRAINT ck_gov_admin_master_district_scope;
ALTER TABLE portal.gov_admin_master ADD CONSTRAINT ck_gov_admin_master_scope CHECK (
    (tier = 'DistrictHead' AND assigned_district IS NOT NULL AND assigned_taluk IS NULL AND assigned_department IS NULL) OR
    (tier = 'TalukHead' AND assigned_district IS NOT NULL AND assigned_taluk IS NOT NULL AND assigned_department IS NULL) OR
    (tier = 'Department' AND assigned_department IS NOT NULL AND assigned_district IS NULL AND assigned_taluk IS NULL) OR
    (tier IN ('Superadmin', 'ChiefMinister') AND assigned_district IS NULL AND assigned_taluk IS NULL AND assigned_department IS NULL)
);
