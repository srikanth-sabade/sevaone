-- Superadmin catalogue management. These functions keep writes in Postgres transactions and
-- deliberately soft-delete entities so applications and audit history remain intact.

DROP FUNCTION IF EXISTS discovery.sp_list_catalog_admin_departments();
CREATE OR REPLACE FUNCTION discovery.sp_list_catalog_admin_departments()
RETURNS TABLE (
    "Id" uuid, "Code" varchar(64), "Name" varchar(200), "DepartmentNameKannada" varchar(200),
    "DepartmentUrl" text, "DepartmentUrlKannada" text, "DisplayOrder" integer,
    "IsActive" boolean, "ServiceCount" integer)
LANGUAGE sql STABLE AS $$
    SELECT d.id, d.department_id, d.department_name, d.department_name_kannada,
           d.department_url, d.department_url_kannada, d.display_order, d.is_active,
           count(s.id)::integer
    FROM discovery.department_master d
    LEFT JOIN discovery.sevaone_service_master s ON s.department_id = d.id
    WHERE d.hierarchy_level = 1
    GROUP BY d.id
    ORDER BY d.display_order, d.department_name;
$$;

DROP FUNCTION IF EXISTS discovery.sp_get_catalog_admin_department(text);
CREATE OR REPLACE FUNCTION discovery.sp_get_catalog_admin_department(p_code text)
RETURNS TABLE (
    "Id" uuid, "Code" varchar(64), "Name" varchar(200), "DepartmentNameKannada" varchar(200),
    "DepartmentUrl" text, "DepartmentUrlKannada" text, "DisplayOrder" integer,
    "IsActive" boolean, "ServiceCount" integer)
LANGUAGE sql STABLE AS $$
    SELECT d.id, d.department_id, d.department_name, d.department_name_kannada,
           d.department_url, d.department_url_kannada, d.display_order, d.is_active,
           (SELECT count(*)::integer FROM discovery.sevaone_service_master s WHERE s.department_id = d.id)
    FROM discovery.department_master d
    WHERE lower(trim(d.department_id)) = lower(trim(p_code)) AND d.hierarchy_level = 1;
$$;

CREATE OR REPLACE FUNCTION discovery.sp_admin_upsert_department(
    p_id uuid, p_code varchar(64), p_name varchar(200), p_name_kannada varchar(200),
    p_url text, p_url_kannada text, p_display_order integer)
RETURNS TABLE ("Id" uuid)
LANGUAGE plpgsql AS $$
BEGIN
    RETURN QUERY
    INSERT INTO discovery.department_master (
        id, department_id, department_name, department_name_kannada, department_url,
        department_url_kannada, portal_name, portal_domain, connector_type, is_integrated,
        is_active, display_order, hierarchy_level)
    VALUES (p_id, trim(p_code), trim(p_name), NULLIF(trim(p_name_kannada), ''),
            NULLIF(trim(p_url), ''), NULLIF(trim(p_url_kannada), ''), trim(p_name), '',
            'generic', FALSE, TRUE, COALESCE(p_display_order, 0), 1)
    ON CONFLICT (department_id) DO UPDATE SET
        department_name = EXCLUDED.department_name,
        department_name_kannada = EXCLUDED.department_name_kannada,
        department_url = EXCLUDED.department_url,
        department_url_kannada = EXCLUDED.department_url_kannada,
        display_order = EXCLUDED.display_order
    RETURNING id;
END;
$$;

CREATE OR REPLACE FUNCTION discovery.sp_admin_set_department_active(p_code text, p_is_active boolean)
RETURNS TABLE ("Found" boolean)
LANGUAGE sql AS $$
    WITH changed AS (
        UPDATE discovery.department_master
        SET is_active = p_is_active
        WHERE lower(trim(department_id)) = lower(trim(p_code)) AND hierarchy_level = 1
        RETURNING 1)
    SELECT EXISTS (SELECT 1 FROM changed);
$$;

DROP FUNCTION IF EXISTS discovery.sp_list_catalog_admin_services(text, text, boolean);
CREATE OR REPLACE FUNCTION discovery.sp_list_catalog_admin_services(
    p_department_code text, p_search text, p_is_active boolean)
RETURNS TABLE (
    "Id" uuid, "DepartmentCode" varchar(64), "DepartmentName" varchar(200),
    "ServiceCode" varchar(32), "Name" varchar(300), "NameKannada" varchar(300),
    "Category" text, "IsActive" boolean, "Version" integer)
LANGUAGE sql STABLE AS $$
    SELECT s.id, d.department_id, d.department_name, s.service_id, s.service_name,
           s.service_name_kannada, s.category, s.is_active, s.version
    FROM discovery.sevaone_service_master s
    JOIN discovery.department_master d ON d.id = s.department_id
    WHERE (p_department_code IS NULL OR lower(d.department_id) = lower(trim(p_department_code)))
      AND (p_is_active IS NULL OR s.is_active = p_is_active)
      AND (p_search IS NULL OR trim(p_search) = '' OR
           lower(s.service_id) LIKE '%' || lower(trim(p_search)) || '%' OR
           lower(s.service_name) LIKE '%' || lower(trim(p_search)) || '%' OR
           lower(coalesce(s.service_name_kannada, '')) LIKE '%' || lower(trim(p_search)) || '%')
    ORDER BY d.display_order, d.department_name, s.service_name;
$$;

DROP FUNCTION IF EXISTS discovery.sp_get_catalog_admin_service(text, text);
CREATE OR REPLACE FUNCTION discovery.sp_get_catalog_admin_service(p_department_code text, p_service_code text)
RETURNS TABLE (
    "Id" uuid, "DepartmentCode" varchar(64), "DepartmentName" varchar(200),
    "ServiceCode" varchar(32), "Name" varchar(300), "NameKannada" varchar(300),
    "SubDepartmentName" varchar(200), "ShortDescription" text, "ShortDescriptionKannada" text,
    "Description" text, "DescriptionKannada" text, "IsSakalaEnabled" boolean,
    "SakalaLabelRaw" text, "SakalaDays" integer, "DepartmentFee" numeric(12,2),
    "ServiceCharge" numeric(12,2), "DocumentsJson" text, "DocumentsKannadaJson" text,
    "EligibilityJson" text, "EligibilityKannadaJson" text, "AliasesJson" text,
    "StepsJson" text, "StepsKannadaJson" text, "Jurisdiction" varchar(100), "Category" text,
    "ModeOfService" varchar(20), "DepartmentServiceId" varchar(64), "Portal" text,
    "ServiceUrl" varchar(500), "DeeplinkCitizenUrl" varchar(500),
    "DeeplinkCitizenUrlKannada" varchar(500), "DeeplinkOperatorUrl" varchar(500),
    "HomepageUrl" varchar(500), "IntegrationMethod" text, "SevasindhuServiceId" text,
    "Remarks" text, "IsActive" boolean, "Version" integer, "VisibleRolesJson" text)
LANGUAGE sql STABLE AS $$
    SELECT s.id, d.department_id, d.department_name, s.service_id, s.service_name,
           s.service_name_kannada, sd.sub_department_name, s.short_description,
           s.short_description_kannada, s.description, s.description_kannada,
           s.is_sakala_enabled, s.sakala_label_raw, s.sakala_days, s.department_fee,
           s.service_charge, s.required_documents_json, s.documents_json_kannada,
           s.eligibility_json, s.eligibility_json_kannada, s.aliases_json, s.steps_json,
           s.steps_json_kannada, s.jurisdiction, s.category, s.mode_of_service,
           s.department_service_id, s.portal, s.service_url, s.deeplink_citizen_url,
           s.deeplink_citizen_url_kannada, s.deeplink_operator_url, s.homepage_url,
           s.integration_method, s.sevasindhu_service_id, s.remarks, s.is_active, s.version,
           coalesce(json_agg(cr.role_key ORDER BY cr.role_key) FILTER (WHERE cr.role_key IS NOT NULL), '[]')::text
    FROM discovery.sevaone_service_master s
    JOIN discovery.department_master d ON d.id = s.department_id
    LEFT JOIN discovery.sub_department_master sd ON sd.id = s.sub_department_id
    LEFT JOIN discovery.service_role_visibility srv ON srv.service_id = s.id
    LEFT JOIN discovery.catalog_role cr ON cr.id = srv.role_id
    WHERE lower(d.department_id) = lower(trim(p_department_code))
      AND lower(s.service_id) = lower(trim(p_service_code))
    GROUP BY s.id, d.id, sd.id;
$$;

CREATE OR REPLACE FUNCTION discovery.sp_admin_upsert_service(
    p_id uuid, p_department_code text, p_service_code varchar(32), p_name varchar(300),
    p_name_kannada varchar(300), p_sub_department_name text, p_short_description text,
    p_short_description_kannada text, p_description text, p_description_kannada text,
    p_is_sakala_enabled boolean, p_sakala_label_raw text, p_sakala_days integer,
    p_department_fee numeric(12,2), p_service_charge numeric(12,2), p_documents_json text,
    p_documents_kannada_json text, p_eligibility_json text, p_eligibility_kannada_json text,
    p_aliases_json text, p_steps_json text, p_steps_kannada_json text, p_jurisdiction varchar(100),
    p_category text, p_mode_of_service varchar(20), p_department_service_id varchar(64),
    p_portal text, p_service_url varchar(500), p_deeplink_citizen_url varchar(500),
    p_deeplink_citizen_url_kannada varchar(500), p_deeplink_operator_url varchar(500),
    p_homepage_url varchar(500), p_integration_method text, p_sevasindhu_service_id text,
    p_remarks text, p_is_active boolean)
RETURNS TABLE ("Id" uuid)
LANGUAGE plpgsql AS $$
DECLARE v_department_id uuid; v_sub_department_id uuid;
BEGIN
    SELECT id INTO v_department_id FROM discovery.department_master
    WHERE lower(department_id) = lower(trim(p_department_code)) AND hierarchy_level = 1;
    IF v_department_id IS NULL THEN RAISE EXCEPTION 'Department not found: %', p_department_code USING ERRCODE = 'P0002'; END IF;

    IF nullif(trim(p_sub_department_name), '') IS NOT NULL THEN
        SELECT id INTO v_sub_department_id FROM discovery.sub_department_master
        WHERE department_id = v_department_id AND lower(trim(sub_department_name)) = lower(trim(p_sub_department_name));
        IF v_sub_department_id IS NULL THEN
            v_sub_department_id := gen_random_uuid();
            INSERT INTO discovery.sub_department_master (id, department_id, sub_department_name)
            VALUES (v_sub_department_id, v_department_id, trim(p_sub_department_name));
        END IF;
    END IF;

    RETURN QUERY
    INSERT INTO discovery.sevaone_service_master (
        id, service_id, department_id, sub_department_id, service_name, service_name_kannada,
        short_description, short_description_kannada, description, description_kannada,
        category, sakala_days, is_sakala_enabled, sakala_label_raw, department_fee, service_charge,
        required_documents_json, documents_json_kannada, eligibility_json, eligibility_json_kannada,
        aliases_json, steps_json, steps_json_kannada, jurisdiction, mode_of_service,
        department_service_id, portal, service_url, deeplink_citizen_url,
        deeplink_citizen_url_kannada, deeplink_operator_url, homepage_url, integration_method,
        sevasindhu_service_id, remarks, is_active, version)
    VALUES (
        p_id, trim(p_service_code), v_department_id, v_sub_department_id, trim(p_name),
        nullif(trim(p_name_kannada), ''), nullif(trim(p_short_description), ''),
        nullif(trim(p_short_description_kannada), ''), nullif(trim(p_description), ''),
        nullif(trim(p_description_kannada), ''), coalesce(nullif(trim(p_category), ''), ''),
        coalesce(p_sakala_days, 0), p_is_sakala_enabled, nullif(trim(p_sakala_label_raw), ''),
        p_department_fee, p_service_charge, coalesce(p_documents_json, '[]'),
        coalesce(p_documents_kannada_json, '[]'), coalesce(p_eligibility_json, '[]'),
        coalesce(p_eligibility_kannada_json, '[]'), coalesce(p_aliases_json, '[]'),
        coalesce(p_steps_json, '[]'), coalesce(p_steps_kannada_json, '[]'),
        nullif(trim(p_jurisdiction), ''), nullif(trim(p_mode_of_service), ''),
        nullif(trim(p_department_service_id), ''), nullif(trim(p_portal), ''),
        nullif(trim(p_service_url), ''), nullif(trim(p_deeplink_citizen_url), ''),
        nullif(trim(p_deeplink_citizen_url_kannada), ''), nullif(trim(p_deeplink_operator_url), ''),
        nullif(trim(p_homepage_url), ''), nullif(trim(p_integration_method), ''),
        nullif(trim(p_sevasindhu_service_id), ''), nullif(trim(p_remarks), ''),
        coalesce(p_is_active, TRUE), 1)
    ON CONFLICT (department_id, service_id) DO UPDATE SET
        sub_department_id = EXCLUDED.sub_department_id, service_name = EXCLUDED.service_name,
        service_name_kannada = EXCLUDED.service_name_kannada, short_description = EXCLUDED.short_description,
        short_description_kannada = EXCLUDED.short_description_kannada, description = EXCLUDED.description,
        description_kannada = EXCLUDED.description_kannada, category = EXCLUDED.category,
        sakala_days = EXCLUDED.sakala_days, is_sakala_enabled = EXCLUDED.is_sakala_enabled,
        sakala_label_raw = EXCLUDED.sakala_label_raw, department_fee = EXCLUDED.department_fee,
        service_charge = EXCLUDED.service_charge, required_documents_json = EXCLUDED.required_documents_json,
        documents_json_kannada = EXCLUDED.documents_json_kannada, eligibility_json = EXCLUDED.eligibility_json,
        eligibility_json_kannada = EXCLUDED.eligibility_json_kannada, aliases_json = EXCLUDED.aliases_json,
        steps_json = EXCLUDED.steps_json, steps_json_kannada = EXCLUDED.steps_json_kannada,
        jurisdiction = EXCLUDED.jurisdiction, mode_of_service = EXCLUDED.mode_of_service,
        department_service_id = EXCLUDED.department_service_id, portal = EXCLUDED.portal,
        service_url = EXCLUDED.service_url, deeplink_citizen_url = EXCLUDED.deeplink_citizen_url,
        deeplink_citizen_url_kannada = EXCLUDED.deeplink_citizen_url_kannada,
        deeplink_operator_url = EXCLUDED.deeplink_operator_url, homepage_url = EXCLUDED.homepage_url,
        integration_method = EXCLUDED.integration_method, sevasindhu_service_id = EXCLUDED.sevasindhu_service_id,
        remarks = EXCLUDED.remarks, is_active = EXCLUDED.is_active, version = sevaone_service_master.version + 1
    RETURNING id;
END;
$$;

CREATE OR REPLACE FUNCTION discovery.sp_admin_set_service_active(p_department_code text, p_service_code text, p_is_active boolean)
RETURNS TABLE ("Found" boolean)
LANGUAGE sql AS $$
    WITH changed AS (
        UPDATE discovery.sevaone_service_master s
        SET is_active = p_is_active, version = s.version + 1
        FROM discovery.department_master d
        WHERE d.id = s.department_id AND lower(d.department_id) = lower(trim(p_department_code))
          AND lower(s.service_id) = lower(trim(p_service_code))
        RETURNING 1)
    SELECT EXISTS (SELECT 1 FROM changed);
$$;

DROP FUNCTION IF EXISTS discovery.sp_list_catalog_admin_changes(integer);
CREATE OR REPLACE FUNCTION discovery.sp_list_catalog_admin_changes(p_limit integer)
RETURNS TABLE (
    "Id" uuid, "EntityType" text, "DepartmentCode" varchar(64), "ServiceCode" varchar(32),
    "ChangedAt" timestamp with time zone, "ChangedBy" text, "ChangeType" varchar(32), "Diff" text)
LANGUAGE sql STABLE AS $$
    SELECT l.id, CASE WHEN l.service_id IS NOT NULL THEN 'service'
                      WHEN l.diff::jsonb ->> 'entityType' = 'sub_department' THEN 'sub_department'
                      ELSE 'department' END,
           coalesce(d.department_id, l.diff::jsonb ->> 'departmentCode'), s.service_id,
           l.changed_at, l.changed_by, l.change_type, l.diff
    FROM discovery.service_change_log l
    LEFT JOIN discovery.sevaone_service_master s ON s.id = l.service_id
    LEFT JOIN discovery.department_master d ON d.id = s.department_id
    ORDER BY l.changed_at DESC
    LIMIT greatest(coalesce(p_limit, 50), 1);
$$;
