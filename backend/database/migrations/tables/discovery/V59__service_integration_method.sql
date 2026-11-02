-- Which external redirect mechanism a service's OPERATOR-assisted "Apply now" should use,
-- per Revenue_Services_Integration_Documentation.docx: 'ajsk' is the Service Plus -> .NET
-- redirect (NK5RedirectPage.aspx, UserId + RaisedLoc, no token), 'sakala_token' is the
-- Sakala Token Generation API + STBRedirectToDepartment/CSG.aspx flow (already live for
-- Catalog 2 via SevaSindhuCatalog2Service/SevaSindhuEscomService). NULL (the default for
-- every imported service) means "no special integration -- operator apply behaves exactly
-- like citizen apply", which is deliberately unchanged for every service except the ones
-- below: the priority_10_departments.xlsx import's service_id numbering does not correspond
-- to Seva Sindhu's own service_id space used in SakalaToken_BasedIntegration_ServiceCounts.xlsx
-- (checked directly -- no genuine overlap), so only Revenue's AJSK sub-department -- the one
-- case the integration doc names outright, independent of that service_id list -- is flagged
-- here. The other ~90 Sakala-token services from that reference list need a real mapping onto
-- our imported catalog before they can be flagged the same way.
ALTER TABLE discovery.sevaone_service_master ADD COLUMN integration_method text
    CONSTRAINT ck_sevaone_service_master_integration_method CHECK (integration_method IS NULL OR integration_method IN ('ajsk', 'sakala_token'));

UPDATE discovery.sevaone_service_master s
SET integration_method = 'ajsk'
FROM discovery.sub_department_master sd
JOIN discovery.department_master d ON d.id = sd.department_id
WHERE s.sub_department_id = sd.id
  AND d.department_id = 'revenue'
  AND sd.sub_department_name = 'AJSK (Atal Jana Snehi Kendra)';
