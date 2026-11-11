-- Metadata of a citizen's DigiLocker documents, and the copies of their content kept in object storage.
-- DigiLocker only serves content while the citizen's DigiLocker session is valid, so content is copied to MinIO.
CREATE TABLE portal.digilocker_document
(
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    citizen_id  uuid NOT NULL REFERENCES portal.sevaone_user (id) ON DELETE CASCADE,
    doc_uri     text NOT NULL,
    name        text NOT NULL,
    doc_type    text NOT NULL DEFAULT '',
    description text NOT NULL DEFAULT '',
    issuer      text NOT NULL DEFAULT '',
    upload_date text NOT NULL DEFAULT '',
    formats     text[] NOT NULL DEFAULT '{}',
    synced_at   timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_digilocker_document UNIQUE (citizen_id, doc_uri)
);

CREATE TABLE portal.digilocker_document_file
(
    document_id uuid NOT NULL REFERENCES portal.digilocker_document (id) ON DELETE CASCADE,
    format      text NOT NULL,
    mime        text NOT NULL,
    size_bytes  integer NOT NULL,
    sha256      text NOT NULL,
    storage_key text NOT NULL,
    fetched_at  timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (document_id, format)
);
