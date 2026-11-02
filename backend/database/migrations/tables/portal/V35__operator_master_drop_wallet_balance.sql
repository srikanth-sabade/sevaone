-- Superseded by wallet_id (V20): balance now lives on the external wallet provider, looked up
-- by wallet_id, not tracked as a local ledger column. Drops the ck_operator_master_wallet_balance_non_negative
-- CHECK along with it (Postgres drops a single-column CHECK automatically with its column).
ALTER TABLE portal.operator_master DROP COLUMN wallet_balance;
