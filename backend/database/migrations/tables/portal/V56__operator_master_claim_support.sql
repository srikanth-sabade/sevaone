-- Supports the operator self-claim flow (portal.operator_whitelist, V54/V55): an operator
-- proves they own a whitelisted user_id via mobile OTP, then sets their own credentials --
-- mirrors how citizens prove identity (mobile OTP + DigiLocker) rather than reusing WSO2
-- password auth, which citizens don't actually use for primary login either.
ALTER TABLE portal.operator_master ADD COLUMN mobile_verified_at timestamp with time zone;

-- The whitelist's kiosk_type ("SPEW") doesn't map to G1/K1/B1 (Grama One/Karnataka One/
-- Bangalore One -- see R__seed_operator.sql's own comment for that mapping) -- UNK marks a
-- self-claimed operator whose real kiosk network isn't yet known, rather than guessing one of
-- the three real categories. Revisit once that mapping is available.
ALTER TABLE portal.operator_master DROP CONSTRAINT ck_operator_master_operator_type;
ALTER TABLE portal.operator_master ADD CONSTRAINT ck_operator_master_operator_type
    CHECK (operator_type IN ('G1', 'K1', 'B1', 'UNK'));
