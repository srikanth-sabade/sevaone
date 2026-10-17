-- sevaone_service_document had no natural-key uniqueness, so a re-run of the catalog
-- ingestion pipeline against the same source would duplicate every document row instead of
-- updating it. document_url is the actual identity of a document within a service -- two
-- different real documents don't share a URL, even if their names happen to match.
CREATE UNIQUE INDEX ix_sevaone_service_document_scheme_id_url ON discovery.sevaone_service_document (scheme_id, document_url);
