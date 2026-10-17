CREATE OR REPLACE FUNCTION portal.sp_insert_gov_admin_import_batch(
    p_id uuid, p_source_filename text, p_source_format text, p_uploaded_by text)
RETURNS uuid LANGUAGE sql AS $$
    INSERT INTO portal.gov_admin_import_batch (id, source_filename, source_format, uploaded_by)
    VALUES (p_id, p_source_filename, p_source_format, p_uploaded_by)
    RETURNING id;
$$;

CREATE OR REPLACE FUNCTION portal.sp_update_gov_admin_import_batch(
    p_id uuid, p_status text, p_row_count integer, p_error_count integer,
    p_warning_count integer, p_published_by text)
RETURNS void LANGUAGE plpgsql AS $$
BEGIN
    UPDATE portal.gov_admin_import_batch
    SET status = p_status,
        row_count = COALESCE(p_row_count, row_count),
        error_count = COALESCE(p_error_count, error_count),
        warning_count = COALESCE(p_warning_count, warning_count),
        published_by = COALESCE(p_published_by, published_by),
        published_at = CASE WHEN p_status = 'published' THEN now() ELSE published_at END
    WHERE id = p_id;
END;
$$;

CREATE OR REPLACE FUNCTION portal.sp_get_gov_admin_import_batch(p_id uuid)
RETURNS SETOF portal.gov_admin_import_batch LANGUAGE sql STABLE AS $$
    SELECT * FROM portal.gov_admin_import_batch WHERE id = p_id;
$$;

CREATE OR REPLACE FUNCTION portal.sp_insert_gov_admin_import_row(
    p_id uuid, p_batch_id uuid, p_row_number integer, p_raw_payload jsonb)
RETURNS uuid LANGUAGE sql AS $$
    INSERT INTO portal.gov_admin_import_row (id, batch_id, row_number, raw_payload)
    VALUES (p_id, p_batch_id, p_row_number, p_raw_payload)
    RETURNING id;
$$;

CREATE OR REPLACE FUNCTION portal.sp_update_gov_admin_import_row(
    p_id uuid, p_action text, p_validation_errors jsonb, p_validation_warnings jsonb)
RETURNS void LANGUAGE sql AS $$
    UPDATE portal.gov_admin_import_row
    SET action = p_action, validation_errors = p_validation_errors,
        validation_warnings = p_validation_warnings
    WHERE id = p_id;
$$;

CREATE OR REPLACE FUNCTION portal.sp_list_gov_admin_import_rows(p_batch_id uuid)
RETURNS TABLE (
    id uuid, batch_id uuid, row_number integer, raw_payload jsonb,
    action character varying, validation_errors jsonb, validation_warnings jsonb)
LANGUAGE sql STABLE AS $$
    SELECT id, batch_id, row_number, raw_payload, action, validation_errors, validation_warnings
    FROM portal.gov_admin_import_row WHERE batch_id = p_batch_id ORDER BY row_number;
$$;
