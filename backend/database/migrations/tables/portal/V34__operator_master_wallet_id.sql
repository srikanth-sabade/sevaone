-- Stable identifier for the operator's wallet on the external wallet provider -- SevaOne will
-- call out to that system (balance, other wallet info) keyed on this, once its API contract is
-- available. Nullable: an operator has none until provisioned there.
ALTER TABLE portal.operator_master ADD COLUMN wallet_id character varying(100);
CREATE UNIQUE INDEX ix_operator_master_wallet_id ON portal.operator_master (wallet_id) WHERE wallet_id IS NOT NULL;
