-- Sakala Token Generation API calls a service by SEVA SINDHU'S OWN service_id, which is a
-- completely different numbering space from priority_10_departments.xlsx's own service_id
-- (the column already stored in department_service_id) -- checked directly against
-- SakalaToken_BasedIntegration_ServiceCounts.xlsx's ~90 listed services: no genuine ID overlap,
-- and most of that list's departments (BESCOM, CESCOM, MESCOM, Forest, KIADB, Urban
-- Development, Tribal Welfare, ...) aren't even among our 10 imported departments. Cross-
-- matching by (department, service name) found exactly 6 unambiguous, same-sub-department
-- matches, all in Agriculture -- every other candidate either had no real match or only a
-- superficial name match landing in the wrong sub-department, and was deliberately left
-- unmapped rather than guessed.
ALTER TABLE discovery.sevaone_service_master ADD COLUMN sevasindhu_service_id text;

UPDATE discovery.sevaone_service_master s
SET sevasindhu_service_id = mapping.sevasindhu_id,
    integration_method = 'sakala_token'
FROM discovery.department_master d,
     (VALUES
        ('2594', '2327'), -- License to sell, Stock or Exhibit for sale or Distribute Insecticides
        ('2591', '2318'), -- Issue of District Level Wholesale Fertilizer License
        ('2593', '2278'), -- Issue of state level Wholesale Fertilizer License
        ('2596', '2322'), -- Retail Fertilizer Market License
        ('2597', '2324'), -- Seed Dealers License
        ('2598', '2325')  -- State level seed sales license
     ) AS mapping(our_department_service_id, sevasindhu_id)
WHERE d.id = s.department_id
  AND d.department_id = 'agriculture'
  AND s.department_service_id = mapping.our_department_service_id;
