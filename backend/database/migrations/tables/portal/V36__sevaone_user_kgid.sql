-- KGID (Karnataka Government ID) -- set only by backend ingestion when provisioning a
-- government official ahead of their first login (not editable via any UI) -- lets that
-- pre-created row be recognized/linked once the official actually logs in and completes their
-- profile. Optional: most sevaone_user rows are ordinary citizens, never officials.
ALTER TABLE portal.sevaone_user ADD COLUMN kgid character varying(50);
CREATE UNIQUE INDEX ix_sevaone_user_kgid ON portal.sevaone_user (kgid)
    WHERE kgid IS NOT NULL;
