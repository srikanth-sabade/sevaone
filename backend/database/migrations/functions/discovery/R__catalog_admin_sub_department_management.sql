-- Superadmin sub-department management. Rows are deactivated, never deleted, so services
-- and catalogue history keep their stable foreign keys.

DROP FUNCTION IF EXISTS discovery.sp_list_sub_departments_for_admin(text, text, boolean);
CREATE OR REPLACE FUNCTION discovery.sp_list_sub_departments_for_admin(
    p_department_code text, p_search text, p_is_active boolean)
RETURNS TABLE (
    "Id" uuid, "DepartmentCode" varchar(64), "DepartmentName" varchar(200),
    "Name" varchar(200), "NameKannada" varchar(200), "DisplayOrder" integer,
    "IsActive" boolean)
LANGUAGE sql STABLE AS $$
    SELECT sd.id, d.department_id, d.department_name, sd.sub_department_name,
           sd.sub_department_name_kannada, sd.display_order, sd.is_active
    FROM discovery.sub_department_master sd
    JOIN discovery.department_master d ON d.id = sd.department_id
    WHERE d.hierarchy_level = 1
      AND (p_department_code IS NULL OR lower(trim(d.department_id)) = lower(trim(p_department_code)))
      AND (p_is_active IS NULL OR sd.is_active = p_is_active)
      AND (p_search IS NULL OR trim(p_search) = '' OR
           lower(sd.sub_department_name) LIKE '%' || lower(trim(p_search)) || '%' OR
           lower(coalesce(sd.sub_department_name_kannada, '')) LIKE '%' || lower(trim(p_search)) || '%' OR
           lower(d.department_name) LIKE '%' || lower(trim(p_search)) || '%')
    ORDER BY d.display_order, d.department_name, sd.display_order, sd.sub_department_name;
$$;

DROP FUNCTION IF EXISTS discovery.sp_get_sub_department_for_admin(uuid);
CREATE OR REPLACE FUNCTION discovery.sp_get_sub_department_for_admin(p_id uuid)
RETURNS TABLE (
    "Id" uuid, "DepartmentCode" varchar(64), "DepartmentName" varchar(200),
    "Name" varchar(200), "NameKannada" varchar(200), "DisplayOrder" integer,
    "IsActive" boolean)
LANGUAGE sql STABLE AS $$
    SELECT sd.id, d.department_id, d.department_name, sd.sub_department_name,
           sd.sub_department_name_kannada, sd.display_order, sd.is_active
    FROM discovery.sub_department_master sd
    JOIN discovery.department_master d ON d.id = sd.department_id
    WHERE sd.id = p_id AND d.hierarchy_level = 1;
$$;

CREATE OR REPLACE FUNCTION discovery.sp_admin_upsert_sub_department(
    p_id uuid, p_department_code text, p_name varchar(200), p_name_kannada varchar(200),
    p_display_order integer, p_is_active boolean)
RETURNS TABLE ("Id" uuid)
LANGUAGE plpgsql AS $$
DECLARE
    v_department_id uuid;
    v_existing_id uuid;
BEGIN
    SELECT id INTO v_department_id
    FROM discovery.department_master
    WHERE lower(trim(department_id)) = lower(trim(p_department_code)) AND hierarchy_level = 1;
    IF v_department_id IS NULL THEN
        RAISE EXCEPTION 'Department not found: %', p_department_code USING ERRCODE = 'P0002';
    END IF;

    SELECT id INTO v_existing_id
    FROM discovery.sub_department_master
    WHERE department_id = v_department_id
      AND lower(trim(sub_department_name)) = lower(trim(p_name))
      AND id <> p_id;
    IF v_existing_id IS NOT NULL THEN
        RAISE EXCEPTION 'Sub-department already exists: %', p_name USING ERRCODE = '23505';
    END IF;

    RETURN QUERY
    INSERT INTO discovery.sub_department_master (
        id, department_id, sub_department_name, sub_department_name_kannada, display_order, is_active)
    VALUES (
        p_id, v_department_id, trim(p_name), nullif(trim(p_name_kannada), ''),
        greatest(coalesce(p_display_order, 0), 0), coalesce(p_is_active, true))
    ON CONFLICT (id) DO UPDATE SET
        sub_department_name = EXCLUDED.sub_department_name,
        sub_department_name_kannada = EXCLUDED.sub_department_name_kannada,
        display_order = EXCLUDED.display_order,
        is_active = EXCLUDED.is_active
    RETURNING id;
END;
$$;

CREATE OR REPLACE FUNCTION discovery.sp_admin_set_sub_department_active(p_id uuid, p_is_active boolean)
RETURNS TABLE ("Found" boolean)
LANGUAGE sql AS $$
    WITH changed AS (
        UPDATE discovery.sub_department_master
        SET is_active = p_is_active
        WHERE id = p_id
        RETURNING 1)
    SELECT EXISTS (SELECT 1 FROM changed);
$$;
