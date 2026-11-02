-- Operator integrations from the supplied Sakala service list and Revenue documentation.
-- Citizen visibility and citizen integration flags are unchanged.
UPDATE discovery.sevaone_service_master s SET integration_method='ajsk'
FROM discovery.sub_department_master sd, discovery.department_master d
WHERE s.sub_department_id=sd.id AND s.department_id=d.id
AND d.department_id='revenue' AND trim(sd.sub_department_name)='AJSK (Atal Jana Snehi Kendra)';

INSERT INTO discovery.sevaone_service_master
(id,service_id,department_id,service_name,category,sakala_days,description,sevasindhu_service_id,integration_method,
required_documents_json,documents_json_kannada,eligibility_json,eligibility_json_kannada,steps_json,steps_json_kannada,aliases_json,is_active)
SELECT gen_random_uuid(),v.code,d.id,v.name,'General',0,v.name,v.sid,v.method,'[]','[]','[]','[]','[]','[]','[]',TRUE
FROM (VALUES
('agriculture', 'AGRICULTURE-2327', 'License to sell, Stock or Exhibit for sale or Distribute Insecticides', '2327', 'sakala_token'),
('agriculture', 'AGRICULTURE-2318', 'Issue of District Level Wholesale Fertilizer License.', '2318', 'sakala_token'),
('agriculture', 'AGRICULTURE-2322', 'Retail Fertilizer Market License.', '2322', 'sakala_token'),
('agriculture', 'AGRICULTURE-2324', 'Seed Dealer’s License.', '2324', 'sakala_token'),
('agriculture', 'AGRICULTURE-2325', 'State level seed sales license.', '2325', 'sakala_token'),
('agriculture', 'AGRICULTURE-2278', 'Issue of state level Wholesale Fertilizer License', '2278', 'sakala_token'),
('cescom', 'CESCOM-33333', 'Load Change', '33333', 'sakala_token_escom'),
('cescom', 'CESCOM-22222', 'Name Change', '22222', 'sakala_token_escom'),
('cescom', 'CESCOM-11111', 'Tariff Change', '11111', 'sakala_token_escom')
) AS v(dept,code,name,sid,method)
JOIN discovery.department_master d ON d.department_id=v.dept
WHERE NOT EXISTS (SELECT 1 FROM discovery.sevaone_service_master s WHERE s.department_id=d.id AND s.service_id=v.code);

INSERT INTO discovery.service_role_visibility(service_id,role_id,granted_by)
SELECT s.id,r.id,'system:operator-integration'
FROM discovery.sevaone_service_master s
JOIN discovery.catalog_role r ON r.role_key='operator'
WHERE s.integration_method IN ('ajsk','sakala_token','sakala_token_escom')
ON CONFLICT(service_id,role_id) DO NOTHING;
