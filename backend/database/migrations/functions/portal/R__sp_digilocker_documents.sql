CREATE OR REPLACE FUNCTION portal.sp_upsert_digilocker_document(
    p_citizen_id uuid, p_doc_uri text, p_name text, p_doc_type text, p_description text,
    p_issuer text, p_upload_date text, p_formats text[])
RETURNS uuid
LANGUAGE sql AS $$
    INSERT INTO portal.digilocker_document (citizen_id, doc_uri, name, doc_type, description, issuer, upload_date, formats)
    VALUES (p_citizen_id, p_doc_uri, p_name, p_doc_type, p_description, p_issuer, p_upload_date, p_formats)
    ON CONFLICT (citizen_id, doc_uri) DO UPDATE SET
        name = EXCLUDED.name, doc_type = EXCLUDED.doc_type, description = EXCLUDED.description,
        issuer = EXCLUDED.issuer, upload_date = EXCLUDED.upload_date, formats = EXCLUDED.formats, synced_at = now()
    RETURNING id;
$$;

CREATE OR REPLACE FUNCTION portal.sp_list_digilocker_documents(p_citizen_id uuid)
RETURNS TABLE
(
    id uuid, doc_uri text, name text, doc_type text, description text, issuer text,
    upload_date text, formats text[], stored_formats text[], synced_at timestamptz
)
LANGUAGE sql STABLE AS $$
    SELECT d.id, d.doc_uri, d.name, d.doc_type, d.description, d.issuer, d.upload_date, d.formats,
           COALESCE((SELECT array_agg(f.format ORDER BY f.format) FROM portal.digilocker_document_file f WHERE f.document_id = d.id), '{}'),
           d.synced_at
    FROM portal.digilocker_document d
    WHERE d.citizen_id = p_citizen_id
    ORDER BY d.name;
$$;

CREATE OR REPLACE FUNCTION portal.sp_save_digilocker_document_file(
    p_document_id uuid, p_format text, p_mime text, p_size_bytes integer, p_sha256 text, p_storage_key text)
RETURNS void
LANGUAGE sql AS $$
    INSERT INTO portal.digilocker_document_file (document_id, format, mime, size_bytes, sha256, storage_key)
    VALUES (p_document_id, p_format, p_mime, p_size_bytes, p_sha256, p_storage_key)
    ON CONFLICT (document_id, format) DO UPDATE SET
        mime = EXCLUDED.mime, size_bytes = EXCLUDED.size_bytes, sha256 = EXCLUDED.sha256,
        storage_key = EXCLUDED.storage_key, fetched_at = now();
$$;

-- Stored file for one of the citizen's own documents; no row when the document is not theirs or the format was never stored.
CREATE OR REPLACE FUNCTION portal.sp_get_digilocker_document_file(p_citizen_id uuid, p_document_id uuid, p_format text)
RETURNS TABLE (name text, mime text, storage_key text)
LANGUAGE sql STABLE AS $$
    SELECT d.name, f.mime, f.storage_key
    FROM portal.digilocker_document d
    JOIN portal.digilocker_document_file f ON f.document_id = d.id AND f.format = p_format
    WHERE d.id = p_document_id AND d.citizen_id = p_citizen_id;
$$;
