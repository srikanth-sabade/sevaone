-- fee (legacy free text, e.g. "Stamp duty + ₹500") is superseded by the numeric
-- department_fee/service_charge split (V23). It was kept temporarily for backward compat with
-- the citizen-facing catalog and the operator fee-collection flow, both of which are being
-- switched over to the numeric fields in the same change that drops this column.
ALTER TABLE discovery.sevaone_service_master DROP COLUMN fee;

-- is_mandatory at the service level duplicated a concept that already exists correctly, one
-- level down, on sevaone_service_document -- the source sheet's "Is it mandatory" column sat
-- immediately after "Required Documents", describing the documents, not the service itself.
-- Never populated on any real row.
ALTER TABLE discovery.sevaone_service_master DROP COLUMN is_mandatory;
