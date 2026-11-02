-- R__seed_test_citizens.sql
--
-- Repeatable Flyway migration: 3 fake citizen accounts for department integration testing only
-- (see PortalApplyService.CreateTestApplyTokenAsync / POST /api/v1/dept/test-token). Idempotent
-- via ON CONFLICT DO NOTHING against the natural business key (wso2_subject), not the surrogate
-- uuid. These never log in through WSO2 -- wso2_subject is a fixed placeholder string, not a real
-- WSO2 IS subject claim, and mobile/aadhaar are left NULL so they can never collide with the
-- partial-unique indexes a real citizen's verified mobile/Aadhaar would hit.
--
-- Only the first (-01) is wired up today (CreateTestApplyTokenAsync always mints for it); -02
-- and -03 are seeded ahead of any product need to pick between them.

INSERT INTO portal.sevaone_user (id, wso2_subject, username, name, seva_id, created_at, updated_at)
VALUES
    ('b3f1a2c4-1111-4a5b-9c6d-000000000001', 'test-citizen-dept-integration-01', 'test-citizen-01', 'Test Citizen One', 'TESTCTZ0001', now(), now()),
    ('b3f1a2c4-1111-4a5b-9c6d-000000000002', 'test-citizen-dept-integration-02', 'test-citizen-02', 'Test Citizen Two', 'TESTCTZ0002', now(), now()),
    ('b3f1a2c4-1111-4a5b-9c6d-000000000003', 'test-citizen-dept-integration-03', 'test-citizen-03', 'Test Citizen Three', 'TESTCTZ0003', now(), now())
ON CONFLICT (wso2_subject) DO NOTHING;
