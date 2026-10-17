-- Per-service redirect targets for the apply handoff.
-- apply_service_url only ever meant "citizen deep link" -- renamed to say that outright, since a
-- service can now also carry a separate operator deep link and a plain homepage fallback for
-- departments that aren't integrated yet. RENAME preserves the one real live value (SVC001).
ALTER TABLE discovery.sevaone_service_master RENAME COLUMN apply_service_url TO deeplink_citizen_url;
ALTER TABLE discovery.sevaone_service_master ADD COLUMN deeplink_operator_url character varying(500);
ALTER TABLE discovery.sevaone_service_master ADD COLUMN homepage_url character varying(500);
