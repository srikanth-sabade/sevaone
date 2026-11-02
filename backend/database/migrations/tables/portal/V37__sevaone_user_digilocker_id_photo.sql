-- digilocker_id: the CeG DigiLocker account id, pulled out of the "digilocker:"-prefixed
-- wso2_subject into its own column. wso2_subject is left as-is (still the lookup key), this is
-- additive for anything that needs the bare id directly.
-- photo: JPEG bytes from eAadhaar's Pht element, when DigiLocker returns one. Set only via the
-- DigiLocker login path, same gating as aadhaar_hash/aadhaar_masked -- first-verified-wins,
-- never silently overwritten.
ALTER TABLE portal.sevaone_user ADD COLUMN digilocker_id character varying(64);
ALTER TABLE portal.sevaone_user ADD COLUMN photo bytea;
CREATE UNIQUE INDEX ix_sevaone_user_digilocker_id ON portal.sevaone_user (digilocker_id)
    WHERE digilocker_id IS NOT NULL;
