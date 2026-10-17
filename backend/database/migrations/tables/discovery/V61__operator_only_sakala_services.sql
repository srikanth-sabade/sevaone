-- Operator-only Sakala-token services: no matching entry in priority_10_departments.xlsx
-- (checked directly, see V60), but a real Seva Sindhu service_id exists from
-- SakalaToken_BasedIntegration_ServiceCounts.xlsx. Per explicit instruction: import them
-- anyway, departments going beyond the original 10, visible to operators only (never
-- citizens) since there is no citizen-facing generic apply link for any of them --
-- sp_list_departments_for_role's EXISTS filter (updated alongside this migration) keeps
-- them out of the citizen catalog entirely, not just their services.
-- Excluded: 3 CESCOM rows with placeholder-looking IDs (33333/22222/11111) duplicating
-- real CESCOM rows already included (2346/2351/2355) under different, real names.
-- No category/description/sakala_days data exists in the source for any of these --
-- 'General'/0/service_name are placeholders, not invented facts about the service.

ALTER TABLE discovery.sevaone_service_master DROP CONSTRAINT ck_sevaone_service_master_integration_method;
ALTER TABLE discovery.sevaone_service_master ADD CONSTRAINT ck_sevaone_service_master_integration_method
    CHECK (integration_method IS NULL OR integration_method IN ('ajsk', 'sakala_token', 'sakala_token_escom'));

-- V__ (versioned) migrations always run before R__ (repeatable) ones on a genuinely
-- fresh database -- confirmed by testing this migration against one: on a fresh DB,
-- R__seed_lookup.sql (which creates 'education'/'revenue'/'social-welfare') had not run
-- yet when this reached the INSERT below, and the department JOIN silently dropped every
-- row targeting them (INNER JOIN, no error). These 3 defensive inserts use the exact same
-- values R__seed_lookup.sql defines, ON CONFLICT DO NOTHING -- a harmless no-op on any
-- environment where seed_lookup already ran (i.e. everywhere this has been applied so far),
-- and the fix that makes this migration actually work on a fresh one.
INSERT INTO discovery.department_master (id, department_id, department_name, portal_name, portal_domain, connector_type, is_integrated, is_active)
VALUES
    (gen_random_uuid(), 'revenue', 'Revenue Department', 'Nadakacheri / Atalji Janasnehi Directorate', 'nadakacheri.karnataka.gov.in', 'revenue', TRUE, TRUE),
    (gen_random_uuid(), 'social-welfare', 'Social Welfare', 'Samaj Kalyana e-Seva', 'sw.kar.nic.in', 'generic', TRUE, TRUE),
    (gen_random_uuid(), 'education', 'Education Department', 'SATS / School Education Portal', 'schooleducation.kar.nic.in', 'generic', TRUE, TRUE)
ON CONFLICT (department_id) DO NOTHING;

-- New top-level departments (operator-only kiosk services, is_integrated = FALSE --
-- no real connector behind them, only the Sakala-token redirect).
INSERT INTO discovery.department_master (id, department_id, department_name, portal_name, portal_domain, connector_type, is_integrated, is_active, display_order)
VALUES (gen_random_uuid(), 'bescom', 'BESCOM (Bangalore Electricity Supply Company)', 'BESCOM (Bangalore Electricity Supply Company)', '', 'generic', FALSE, TRUE, 11)
ON CONFLICT (department_id) DO NOTHING;

INSERT INTO discovery.department_master (id, department_id, department_name, portal_name, portal_domain, connector_type, is_integrated, is_active, display_order)
VALUES (gen_random_uuid(), 'cescom', 'CESCOM (Chamundeshwari Electricity Supply Corporation)', 'CESCOM (Chamundeshwari Electricity Supply Corporation)', '', 'generic', FALSE, TRUE, 12)
ON CONFLICT (department_id) DO NOTHING;

INSERT INTO discovery.department_master (id, department_id, department_name, portal_name, portal_domain, connector_type, is_integrated, is_active, display_order)
VALUES (gen_random_uuid(), 'mescom', 'MESCOM (Mangalore Electricity Supply Company)', 'MESCOM (Mangalore Electricity Supply Company)', '', 'generic', FALSE, TRUE, 13)
ON CONFLICT (department_id) DO NOTHING;

INSERT INTO discovery.department_master (id, department_id, department_name, portal_name, portal_domain, connector_type, is_integrated, is_active, display_order)
VALUES (gen_random_uuid(), 'bwssb', 'BWSSB (Bangalore Water Supply and Sewerage Board)', 'BWSSB (Bangalore Water Supply and Sewerage Board)', '', 'generic', FALSE, TRUE, 14)
ON CONFLICT (department_id) DO NOTHING;

INSERT INTO discovery.department_master (id, department_id, department_name, portal_name, portal_domain, connector_type, is_integrated, is_active, display_order)
VALUES (gen_random_uuid(), 'forest', 'Forest Department', 'Forest Department', '', 'generic', FALSE, TRUE, 15)
ON CONFLICT (department_id) DO NOTHING;

INSERT INTO discovery.department_master (id, department_id, department_name, portal_name, portal_domain, connector_type, is_integrated, is_active, display_order)
VALUES (gen_random_uuid(), 'egovernance', 'Centre for e-Governance', 'Centre for e-Governance', '', 'generic', FALSE, TRUE, 16)
ON CONFLICT (department_id) DO NOTHING;

INSERT INTO discovery.department_master (id, department_id, department_name, portal_name, portal_domain, connector_type, is_integrated, is_active, display_order)
VALUES (gen_random_uuid(), 'itbt', 'IT, BT and Science & Technology Department', 'IT, BT and Science & Technology Department', '', 'generic', FALSE, TRUE, 17)
ON CONFLICT (department_id) DO NOTHING;

INSERT INTO discovery.department_master (id, department_id, department_name, portal_name, portal_domain, connector_type, is_integrated, is_active, display_order)
VALUES (gen_random_uuid(), 'kiadb', 'KIADB (Karnataka Industrial Area Development Board)', 'KIADB (Karnataka Industrial Area Development Board)', '', 'generic', FALSE, TRUE, 18)
ON CONFLICT (department_id) DO NOTHING;

INSERT INTO discovery.department_master (id, department_id, department_name, portal_name, portal_domain, connector_type, is_integrated, is_active, display_order)
VALUES (gen_random_uuid(), 'legal-metrology', 'Department of Legal Metrology', 'Department of Legal Metrology', '', 'generic', FALSE, TRUE, 19)
ON CONFLICT (department_id) DO NOTHING;

INSERT INTO discovery.department_master (id, department_id, department_name, portal_name, portal_domain, connector_type, is_integrated, is_active, display_order)
VALUES (gen_random_uuid(), 'nursing-exam-board', 'Karnataka State Diploma in Nursing Examination Board', 'Karnataka State Diploma in Nursing Examination Board', '', 'generic', FALSE, TRUE, 20)
ON CONFLICT (department_id) DO NOTHING;

INSERT INTO discovery.department_master (id, department_id, department_name, portal_name, portal_domain, connector_type, is_integrated, is_active, display_order)
VALUES (gen_random_uuid(), 'tribal-welfare', 'Department of Tribal Welfare', 'Department of Tribal Welfare', '', 'generic', FALSE, TRUE, 21)
ON CONFLICT (department_id) DO NOTHING;

INSERT INTO discovery.department_master (id, department_id, department_name, portal_name, portal_domain, connector_type, is_integrated, is_active, display_order)
VALUES (gen_random_uuid(), 'youth-empowerment', 'Department of Youth Empowerment and Sports', 'Department of Youth Empowerment and Sports', '', 'generic', FALSE, TRUE, 22)
ON CONFLICT (department_id) DO NOTHING;

-- 'urban': on an already-migrated environment this row exists but was deactivated in V58
-- (not part of the xlsx's 10) -- on a fresh DB it doesn't exist at all any more (dropped from
-- R__seed_lookup.sql for the same reason). Handle both: create it fresh if missing (same
-- operator-only shape as the 12 departments above), then unconditionally (re)activate it.
INSERT INTO discovery.department_master (id, department_id, department_name, portal_name, portal_domain, connector_type, is_integrated, is_active, display_order)
VALUES (gen_random_uuid(), 'urban', 'Urban Development', 'Urban Development', '', 'generic', FALSE, TRUE, 23)
ON CONFLICT (department_id) DO NOTHING;
UPDATE discovery.department_master SET is_active = TRUE, display_order = 23 WHERE department_id = 'urban';

-- New sub-departments under the already-existing Education department (Revenue's SSLR
-- and Social Welfare's core sub-department already exist and are reused as-is).
INSERT INTO discovery.sub_department_master (id, department_id, sub_department_name, is_active, display_order)
SELECT gen_random_uuid(), d.id, v.name, TRUE, 0
FROM discovery.department_master d, (VALUES ('Department of Primary Education'), ('Department of Public Instruction')) AS v(name)
WHERE d.department_id = 'education'
ON CONFLICT (department_id, sub_department_name) DO NOTHING;

INSERT INTO discovery.sevaone_service_master
    (id, service_id, department_id, sub_department_id, service_name, category, sakala_days, description,
     sevasindhu_service_id, integration_method,
     required_documents_json, documents_json_kannada, eligibility_json, eligibility_json_kannada, steps_json, steps_json_kannada,
     aliases_json, is_active)
SELECT gen_random_uuid(), v.service_code, d.id, sd.id, v.service_name, 'General', 0, v.service_name,
       v.sevasindhu_id, v.method,
       '[]', '[]', '[]', '[]', '[]', '[]', '[]', TRUE
FROM (VALUES
    ('bescom', NULL, 'BESCOM-2362', 'New Connection', '2362', 'sakala_token_escom'),
    ('bescom', NULL, 'BESCOM-2371', 'Name Change - RAPDRP - BESCOM Token', '2371', 'sakala_token_escom'),
    ('cescom', NULL, 'CESCOM-2346', 'Load Change', '2346', 'sakala_token_escom'),
    ('cescom', NULL, 'CESCOM-2351', 'Name Change - NonRAPDRP', '2351', 'sakala_token_escom'),
    ('cescom', NULL, 'CESCOM-2355', 'Tariff Change', '2355', 'sakala_token_escom'),
    ('cescom', NULL, 'CESCOM-2252', 'New Connection', '2252', 'sakala_token_escom'),
    ('cescom', NULL, 'CESCOM-2385', 'Name Change - RAPDRP', '2385', 'sakala_token_escom'),
    ('mescom', NULL, 'MESCOM-2394', 'LT New Connection-NonRAPDRP', '2394', 'sakala_token_escom'),
    ('mescom', NULL, 'MESCOM-2399', 'HT New Connection-NonRAPDRP', '2399', 'sakala_token_escom'),
    ('education', 'Department of Primary Education', 'EDUCATION-2364', 'Renewal of recognition', '2364', 'sakala_token'),
    ('bwssb', NULL, 'BWSSB-2248', 'Clearing of blockage of Under Ground Drainage (UGD) pipelines and replacement of Damaged/missing Manhole Cover', '2248', 'sakala_token'),
    ('bwssb', NULL, 'BWSSB-2245', 'Certificate of non-availability of water from water supply agency required for NOC for water abstraction from CGWA', '2245', 'sakala_token'),
    ('bwssb', NULL, 'BWSSB-2246', 'Testing of Faulty Meters', '2246', 'sakala_token'),
    ('bwssb', NULL, 'BWSSB-2244', 'Drilling and Borewell', '2244', 'sakala_token'),
    ('cescom', NULL, 'CESCOM-2345', 'NCMS', '2345', 'sakala_token_escom'),
    ('education', 'Department of Public Instruction', 'EDUCATION-2380', 'Disposal of application for Registration of new educational institutions', '2380', 'sakala_token'),
    ('education', 'Department of Public Instruction', 'EDUCATION-2378', 'Issue of Duplicate SSLC Marks Card', '2378', 'sakala_token'),
    ('forest', NULL, 'FOREST-2396', 'Sanction of ex-gratia to the affected in case of Human death/permanent disability caused due to wild animal', '2396', 'sakala_token'),
    ('forest', NULL, 'FOREST-2395', 'Sanction of ex-gratia to the effected person in case of Cattle killed by wild animal', '2395', 'sakala_token'),
    ('forest', NULL, 'FOREST-2210', 'Sanction of ex-gratia to the affected person in case of crop damage caused by wild animals', '2210', 'sakala_token'),
    ('education', 'Department of Public Instruction', 'EDUCATION-2374', 'Photocopy of Answers Script SSLC for revaluation purpose', '2374', 'sakala_token'),
    ('education', 'Department of Public Instruction', 'EDUCATION-2448', 'Application for Photo Copy (Other Exam)', '2448', 'sakala_token'),
    ('egovernance', NULL, 'EGOVERNANCE-2466', 'Integrated Public Grievance Redressal System/iPGRS', '2466', 'sakala_token'),
    ('itbt', NULL, 'ITBT-2209', 'Startup Registration', '2209', 'sakala_token'),
    ('kiadb', NULL, 'KIADB-2236', 'Certificate of Non-Availability of Water', '2236', 'sakala_token'),
    ('kiadb', NULL, 'KIADB-2238', 'Sewage Connection', '2238', 'sakala_token'),
    ('legal-metrology', NULL, 'LEGAL_METROLOGY-2299', 'Amendment of Registration Certificate For Manufacturer Of Package Commodities', '2299', 'sakala_token'),
    ('legal-metrology', NULL, 'LEGAL_METROLOGY-2298', 'Amendment of Dealer license of Weights and Measures', '2298', 'sakala_token'),
    ('legal-metrology', NULL, 'LEGAL_METROLOGY-2300', 'Amendment of Registration Certificate For Packer Of Package Commodities', '2300', 'sakala_token'),
    ('legal-metrology', NULL, 'LEGAL_METROLOGY-2301', 'Amendment of Registration Certificate For Importer Of Package Commodities', '2301', 'sakala_token'),
    ('legal-metrology', NULL, 'LEGAL_METROLOGY-2296', 'Amendment of Manufacturer license of Weights and Measures', '2296', 'sakala_token'),
    ('legal-metrology', NULL, 'LEGAL_METROLOGY-2302', 'Amendment of Repairer license of Weights and Measures', '2302', 'sakala_token'),
    ('legal-metrology', NULL, 'LEGAL_METROLOGY-2303', 'Issue of Registration Certificate For Manufacturer of Package Commodities', '2303', 'sakala_token'),
    ('legal-metrology', NULL, 'LEGAL_METROLOGY-2289', 'Issue of Repairer license of Weights and Measures', '2289', 'sakala_token'),
    ('legal-metrology', NULL, 'LEGAL_METROLOGY-2281', 'Issue of Dealer license of Weights and Measures', '2281', 'sakala_token'),
    ('legal-metrology', NULL, 'LEGAL_METROLOGY-2286', 'Issue of Registration Certificate For Packer of Package Commodities', '2286', 'sakala_token'),
    ('legal-metrology', NULL, 'LEGAL_METROLOGY-2292', 'Renewal of Dealer license of Weights and Measures', '2292', 'sakala_token'),
    ('legal-metrology', NULL, 'LEGAL_METROLOGY-2297', 'Issue of Registration Certificate For Importer of Package Commodities', '2297', 'sakala_token'),
    ('legal-metrology', NULL, 'LEGAL_METROLOGY-2284', 'Renewal of Repairer license of Weights and Measures', '2284', 'sakala_token'),
    ('legal-metrology', NULL, 'LEGAL_METROLOGY-2291', 'Renewal of Manufacturer license of Weights and Measures', '2291', 'sakala_token'),
    ('legal-metrology', NULL, 'LEGAL_METROLOGY-2274', 'Issuing of Verification & Stamping Certificate', '2274', 'sakala_token'),
    ('legal-metrology', NULL, 'LEGAL_METROLOGY-2249', 'Issue Of New License To Manufacturer Of Weights & Measures', '2249', 'sakala_token'),
    ('nursing-exam-board', NULL, 'NURSING_EXAM_BOARD-2414', 'Course Completion Certificate, Internship Training Exam Marks Cards and Diploma Certificates', '2414', 'sakala_token'),
    ('nursing-exam-board', NULL, 'NURSING_EXAM_BOARD-2412', 'Photo Copies of 1st year, 2nd year and 3rd year Student’s Answer Scripts', '2412', 'sakala_token'),
    ('nursing-exam-board', NULL, 'NURSING_EXAM_BOARD-2413', 'Photo Copies of 1st Year, 2nd Year and 3rd year General Nursing and Midwifery Exam Answer Scripts', '2413', 'sakala_token'),
    ('education', 'Department of Public Instruction', 'EDUCATION-12121', 'Issue of duplicate Marks card of the following exams conducted by the director (other exams)', '12121', 'sakala_token'),
    ('education', 'Department of Public Instruction', 'EDUCATION-2376', 'Revaluation of Answer Scripts of the following exams conducted by the director (other exams)', '2376', 'sakala_token'),
    ('education', 'Department of Public Instruction', 'EDUCATION-2454', 'Issue of Duplicate PUC Marks Card', '2454', 'sakala_token'),
    ('social-welfare', 'Social Welfare Department (core)', 'SOCIAL_WELFARE-2459', 'Post Metric Scholarship', '2459', 'sakala_token'),
    ('social-welfare', 'Social Welfare Department (core)', 'SOCIAL_WELFARE-2455', 'Pre Metric Scholarship', '2455', 'sakala_token'),
    ('education', 'Department of Public Instruction', 'EDUCATION-2450', 'Photo copying answer scripts of the 2nd PUC final examination', '2450', 'sakala_token'),
    ('education', 'Department of Public Instruction', 'EDUCATION-2452', 'Revaluation of answer scripts of the 2nd PUC final examination', '2452', 'sakala_token'),
    ('education', 'RGUHS (Rajiv Gandhi University of Health Sciences)', 'EDUCATION-1497', 'Application for NOC for Internship Transfer-RGUHS', '1497', 'sakala_token'),
    ('education', 'RGUHS (Rajiv Gandhi University of Health Sciences)', 'EDUCATION-1503', 'Application for Migration Certificate-RGUHS', '1503', 'sakala_token'),
    ('education', 'RGUHS (Rajiv Gandhi University of Health Sciences)', 'EDUCATION-1540', 'Application for NOC Migration Transfer-RGUHS', '1540', 'sakala_token'),
    ('education', 'RGUHS (Rajiv Gandhi University of Health Sciences)', 'EDUCATION-1494', 'Application for Provisional Degree Certificate-RGUHS', '1494', 'sakala_token'),
    ('education', 'Department of Primary Education', 'EDUCATION-112', 'First Recognition', '112', 'sakala_token'),
    ('education', 'Department of Primary Education', 'EDUCATION-115', 'Issue of NOC to start CBSE/ICSE schools', '115', 'sakala_token'),
    ('education', 'Department of Primary Education', 'EDUCATION-111', 'Disposal application for Registration of new educational institution', '111', 'sakala_token'),
    ('education', 'Department of Primary Education', 'EDUCATION-114', 'Disposal of application for registration of new educational institutes (pre-primary)', '114', 'sakala_token'),
    ('education', 'Department of Primary Education', 'EDUCATION-113', 'Renewal of recognition (Primary)', '113', 'sakala_token'),
    ('revenue', 'SSLR (Department of Survey, Settlement and Land Records)', 'REVENUE-2315', 'Change of Khatha (Undisputed Cases)', '2315', 'sakala_token'),
    ('education', 'Department of Public Instruction', 'EDUCATION-2379', 'S.S.L.C Fail Marks card with Date of Birth', '2379', 'sakala_token'),
    ('revenue', 'SSLR (Department of Survey, Settlement and Land Records)', 'REVENUE-2306', 'Updation of land Acquisition details in RTC', '2306', 'sakala_token'),
    ('revenue', 'SSLR (Department of Survey, Settlement and Land Records)', 'REVENUE-2308', 'Updation of land conversion details in RTC', '2308', 'sakala_token'),
    ('revenue', 'SSLR (Department of Survey, Settlement and Land Records)', 'REVENUE-2311', 'Updation of Pledge and release details in RTC', '2311', 'sakala_token'),
    ('education', 'Department of Public Instruction', 'EDUCATION-2377', 'Revaluation of Examination papers SSLC', '2377', 'sakala_token'),
    ('tribal-welfare', NULL, 'TRIBAL_WELFARE-2483', 'Post-metric scholarship', '2483', 'sakala_token'),
    ('tribal-welfare', NULL, 'TRIBAL_WELFARE-2480', 'Pre-metric scholarship', '2480', 'sakala_token'),
    ('urban', NULL, 'URBAN-2256', 'Auto Trade License Renewal', '2256', 'sakala_token'),
    ('urban', NULL, 'URBAN-2253', 'Permission / Approval for Mobile Tower Erection', '2253', 'sakala_token'),
    ('urban', NULL, 'URBAN-2264', 'NOC for Ground Water Usage', '2264', 'sakala_token'),
    ('urban', NULL, 'URBAN-2343', 'New Tap Connection', '2343', 'sakala_token'),
    ('urban', NULL, 'URBAN-2344', 'New Underground Drainage Connection', '2344', 'sakala_token'),
    ('urban', NULL, 'URBAN-2260', 'Permission / NOC for Movie shooting', '2260', 'sakala_token'),
    ('urban', NULL, 'URBAN-2257', 'Road Cutting (Right of Way) Permission for OFC Laying', '2257', 'sakala_token'),
    ('urban', NULL, 'URBAN-2259', 'Renewal of Signage License for Advertisement', '2259', 'sakala_token'),
    ('urban', NULL, 'URBAN-2258', 'Signage License for Advertisement', '2258', 'sakala_token'),
    ('urban', NULL, 'URBAN-2255', 'Providing tap and underground drainage connection', '2255', 'sakala_token'),
    ('urban', NULL, 'URBAN-2254', 'Grant of Trade licence as prescribed', '2254', 'sakala_token'),
    ('youth-empowerment', NULL, 'YOUTH_EMPOWERMENT-2710', 'Bharath Jodo Yuva Sangha Registration', '2710', 'sakala_token')
) AS v(department_code, sub_department_name, service_code, service_name, sevasindhu_id, method)
JOIN discovery.department_master d ON d.department_id = v.department_code
LEFT JOIN discovery.sub_department_master sd ON sd.department_id = d.id AND sd.sub_department_name = v.sub_department_name
ON CONFLICT (department_id, service_id) DO NOTHING;

-- Same fresh-DB ordering issue as the department bootstrap above: catalog_role is seeded
-- by R__seed_catalog_role.sql, also a repeatable migration that runs after this one on a
-- fresh database. Same fix, same values, same ON CONFLICT DO NOTHING.
INSERT INTO discovery.catalog_role (id, role_key, display_name, description)
VALUES
    (gen_random_uuid(), 'citizen', 'Citizen', 'The public-facing Service Catalogue at /catalog.'),
    (gen_random_uuid(), 'operator', 'Operator', 'Assisted-mode counters (Karnataka One / Bangalore One).'),
    (gen_random_uuid(), 'admin', 'Admin', 'Catalog administrators -- ingestion, visibility management, audit.')
ON CONFLICT (role_key) DO NOTHING;

-- Grant OPERATOR role visibility only -- deliberately no citizen grant. There is no
-- citizen-facing generic apply link for any of these (source data has no documents/
-- fee/procedure, just a name + Seva Sindhu id), and they were never in the xlsx this
-- catalog was actually built from.
INSERT INTO discovery.service_role_visibility (service_id, role_id, granted_by)
SELECT s.id, r.id, 'system:sakala-import'
FROM discovery.sevaone_service_master s
JOIN discovery.catalog_role r ON r.role_key = 'operator'
WHERE s.integration_method IN ('sakala_token', 'sakala_token_escom')
ON CONFLICT (service_id, role_id) DO NOTHING;
