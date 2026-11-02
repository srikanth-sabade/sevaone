-- R__seed_operator.sql
--
-- Repeatable Flyway migration: one seed operator for local dev/testing. Idempotent via
-- ON CONFLICT DO NOTHING against the natural business key (operator_id), not the surrogate uuid.
-- Password: Operator@123 (PBKDF2-SHA256, 210000 iterations -- see OperatorPasswordHasher.Hash).

INSERT INTO portal.operator_master (id, operator_id, operator_type, operator_name, mobile_no, is_active, password_hash, created_at)
VALUES (
    'a1b2c3d4-e5f6-4789-9abc-def012345701', 'OPR10001', 'K1', 'Test Operator (Karnataka One Counter 1)', '9900011122',
    TRUE,
    '210000.XVsYjZpDDEyOuwGM30FpaQ==.DzbhvFOTpt+SR5bjXo1iT0fq+jJWQHmsnhnJs0oTzNQ=',
    now()
)
ON CONFLICT (operator_id) DO NOTHING;
