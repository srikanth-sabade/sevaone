-- Distinguishes a catalogue-originated apply click from an AI-originated one, so the
-- "Frequently requested" ranking (and any future reporting) can break real usage down by
-- source, not just a single blended count. See sp_list_top_requested_services.
--
-- Every row inserted before this migration came from the catalogue apply flow - AI-driven
-- apply-click tracking never worked (see ApplyClickTrackingService's original callers: the AI
-- chat path only had a real department/service code to send when sevaone-rag's own free-text
-- identifiers happened to line up with the real catalog, which they structurally never do) -
-- so backfilling every existing row as 'catalogue' is factually correct, not a guess.
ALTER TABLE discovery.service_apply_track ADD COLUMN source character varying(16);
UPDATE discovery.service_apply_track SET source = 'catalogue' WHERE source IS NULL;
ALTER TABLE discovery.service_apply_track ALTER COLUMN source SET NOT NULL;
ALTER TABLE discovery.service_apply_track ADD CONSTRAINT ck_service_apply_track_source
    CHECK (source IN ('catalogue', 'ai'));
