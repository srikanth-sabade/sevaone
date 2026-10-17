-- Operators now authenticate against a real WSO2 IS account (password grant), not SevaOne's
-- own PBKDF2 hash -- see backend/docs/WSO2_IS_ROADMAP.md section 2.3 and
-- Wso2OperatorIdentityClient. wso2_subject is the SCIM2 username ("sevaone-operator-{userId}"),
-- same linkage pattern portal.sevaone_user already uses for citizens. Nullable: a pre-seeded
-- legacy operator (e.g. R__seed_operator.sql's OPR10001) has none and keeps logging in via its
-- existing password_hash until migrated -- OperatorAuthService branches on this column's
-- presence rather than requiring every operator to move at once.
ALTER TABLE portal.operator_master ADD COLUMN wso2_subject character varying(100);
CREATE UNIQUE INDEX ix_operator_master_wso2_subject ON portal.operator_master (wso2_subject) WHERE wso2_subject IS NOT NULL;
