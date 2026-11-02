-- eAadhaar's LData element carries the citizen's name/address in their local language
-- (Kannada, lang="07", on Karnataka's CeG DigiLocker gateway) alongside the English Poi/Poa
-- fields SevaOne already captured. Store it too, since Karnataka's own portal has an obvious
-- use for it, and CeG hands it over in the same response at no extra cost.
ALTER TABLE portal.sevaone_user ADD COLUMN name_kannada character varying(200);
ALTER TABLE portal.sevaone_user ADD COLUMN address_kannada character varying(500);
-- Same District/StateName/VillageName split as the English columns -- not folded into
-- address_kannada, mirroring how the English side keeps these separate too.
ALTER TABLE portal.sevaone_user ADD COLUMN district_kannada character varying(120);
ALTER TABLE portal.sevaone_user ADD COLUMN state_kannada character varying(100);
ALTER TABLE portal.sevaone_user ADD COLUMN village_kannada character varying(100);
