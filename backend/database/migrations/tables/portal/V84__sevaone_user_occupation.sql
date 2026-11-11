-- Occupation category picked on the profile page; drives service/scheme recommendations.
-- Free-form code from the API's fixed list (validated in UpdateProfileValidator), not a DB enum, so the list can grow without a migration.
ALTER TABLE portal.sevaone_user ADD COLUMN occupation character varying(40);
