-- Catalog admins are their own persona -- not necessarily the same people as citizen-service
-- counter operators (portal.operator_master), so a separate table rather than an operator_type
-- flag. Same credential approach as operator_master: no WSO2 IS account for internal staff,
-- SevaOne owns the password hash directly (PBKDF2-SHA256, OperatorPasswordHasher -- reused
-- as-is, nothing operator-specific in its implementation despite the name).
CREATE TABLE discovery.catalog_admin_master (
    id uuid NOT NULL,
    admin_id character varying(50) NOT NULL,
    admin_name character varying(200),
    email character varying(200),
    password_hash text,
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_catalog_admin_master PRIMARY KEY (id)
);
CREATE UNIQUE INDEX ix_catalog_admin_master_admin_id ON discovery.catalog_admin_master (admin_id);
