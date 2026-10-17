-- R__seed_catalog_admin.sql
--
-- Repeatable Flyway migration: one seed catalog admin for local dev/testing, mirroring
-- R__seed_operator.sql. Idempotent via ON CONFLICT DO NOTHING against the natural business
-- key (admin_id), not the surrogate uuid.
-- Password: Admin@123 (PBKDF2-SHA256, 210000 iterations -- see OperatorPasswordHasher.Hash).

INSERT INTO discovery.catalog_admin_master (id, admin_id, admin_name, email, password_hash, is_active, created_at)
VALUES (
    'b2c3d4e5-f6a7-4890-9abc-def012345801', 'ADMIN001', 'Catalog Admin', 'catalog-admin@sevaone.local',
    '210000.tyQkm9pVZSugbtUsm1SQSg==.kfg2YhIEdMe0dCZNd5bbifPD9k2ghrwkKu/lyH38iZ4=',
    TRUE, now()
)
ON CONFLICT (admin_id) DO NOTHING;
