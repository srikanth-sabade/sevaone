-- Retired: gov-admin login now looks up by username (stable from creation), not wso2_subject
-- (null until the first-time-login claim completes -- see
-- V69__gov_admin_master_claim_flow.sql). Replaced by
-- R__sp_get_gov_admin_by_username.sql. Kept as a tombstone (rather than deleting the file
-- outright) so Flyway's applied-migration history for this repeatable migration stays valid.
DROP FUNCTION IF EXISTS portal.sp_get_gov_admin_by_wso2_subject(text);
