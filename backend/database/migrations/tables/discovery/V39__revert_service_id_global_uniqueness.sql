-- V38 made service_id globally unique so GetSchemeByServiceCodeAsync (apply-token minting,
-- application submit, the department-completion webhook) could look a scheme up by
-- service_id alone with no department in hand -- a real gap, since two departments reusing a
-- code could silently misroute that lookup.
--
-- Reverted here instead: the Service Catalogue's real, migrated data (754 services from the
-- xlsx source) genuinely has 55 service_id collisions across different departments -- two
-- unrelated services in different departments legitimately sharing a code is not an anomaly
-- in this dataset, it's the norm the source data was built with. A global unique index can't
-- coexist with that data. The actual gap V38 was closing is fixed properly instead, by making
-- every caller of the by-service-code lookup department-aware (see
-- sp_get_scheme_by_department_and_service_code and its callers in
-- PortalApplyService/PortalDeptFacingService/SubmitApplicationService) rather than by
-- constraining the data to fit a lookup that didn't have department context yet.
DROP INDEX discovery.ix_sevaone_service_master_service_id;
CREATE UNIQUE INDEX ix_sevaone_service_master_department_id_service_code
    ON discovery.sevaone_service_master (department_id, service_id);

DROP FUNCTION IF EXISTS discovery.sp_get_scheme_by_service_code(character varying);
