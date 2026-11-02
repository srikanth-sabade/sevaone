-- Denormalized read-only view matching the shape of the eCourt-style integration spec's
-- SEVAONE_SERVICE_DOCUMENT table (flat DEPARTMENT_ID/SERVICE_ID columns) -- for ad-hoc
-- querying/reporting only. The underlying table (discovery.sevaone_service_document) stays
-- normalized (scheme_id FK, no department_id/service_id columns of its own) -- see
-- backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md-adjacent discussion: SevaOne's own code
-- never needs this view, since it always has scheme_id in hand already (GetDocumentsBySchemeAsync).
-- Flyway "R__" repeatable migration: CREATE OR REPLACE VIEW is idempotent, re-applied
-- automatically whenever this file's content changes.
CREATE OR REPLACE VIEW discovery.v_sevaone_service_document_flat AS
SELECT
    doc.id AS sevaone_service_document_id,
    dept.department_id,
    svc.service_id,
    doc.document_name,
    doc.document_url,
    doc.document_type,
    doc.is_active,
    doc.created_at
FROM discovery.sevaone_service_document doc
JOIN discovery.sevaone_service_master svc ON svc.id = doc.scheme_id
JOIN discovery.department_master dept ON dept.id = svc.department_id;
