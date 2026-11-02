-- documents_json had no Kannada counterpart, unlike eligibility_json/steps_json (see V42).
-- Needed because a "Required Documents" checklist (e.g. "Aadhaar Card", "Ration Card") from
-- a source spreadsheet has no URL at all -- it's a different concept from
-- sevaone_service_document's per-row document_url (an AI-readable reference link, NOT NULL
-- by design), not merely a less-structured version of it. Ingestion writes checklist-style
-- documents (no URL available) here; sevaone_service_document stays reserved for sources
-- that actually provide a document_url per document.
ALTER TABLE discovery.sevaone_service_master ADD COLUMN documents_json_kannada text NOT NULL DEFAULT '[]';
