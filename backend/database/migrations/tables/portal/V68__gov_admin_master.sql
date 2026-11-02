-- Government admin panel identity: real WSO2 IS accounts (existing userstore, password grant,
-- SevaOne_GovAdmin SCIM2 group -- see Docker-deploy/wso2/provision.sh), gated the same way
-- portal.operator_master is (see V57__operator_master_wso2_subject.sql). wso2_subject is
-- NOT NULL from day one, unlike operator_master's nullable column -- there is no legacy
-- pre-WSO2 gov-admin account to support.
--
-- tier/assigned_district/assigned_taluk are the actual RBAC data: WSO2 IS 7.3.0 confirmed live
-- to never surface SCIM2 group membership in the OAuth token regardless of flow (see
-- Wso2OperatorIdentityClient's doc comment), so this local table -- not a WSO2 claim -- is the
-- only reliable source of a gov admin's tier and geographic scope. assigned_district/taluk are
-- free text, matched case-insensitively against portal.sevaone_user.district/.taluk and
-- orchestration.applications.district/.taluk at query time (see GovDashboardService) -- both of
-- those are themselves free text with no master table, so this deliberately doesn't introduce one.
CREATE TABLE portal.gov_admin_master (
    id uuid NOT NULL,
    wso2_subject character varying(100) NOT NULL,
    display_name character varying(200) NOT NULL,
    tier character varying(20) NOT NULL,
    assigned_district character varying(120),
    assigned_taluk character varying(120),
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_gov_admin_master PRIMARY KEY (id),
    CONSTRAINT ck_gov_admin_master_tier CHECK (tier IN ('Superadmin', 'ChiefMinister', 'GovtOfficial', 'DistrictHead', 'TalukHead')),
    -- Encodes the hierarchy's scoping rule at the DB level, not just in application code:
    -- District Head must carry a district, Taluk Head must carry both, everyone else neither.
    CONSTRAINT ck_gov_admin_master_district_scope CHECK (
        (tier = 'DistrictHead' AND assigned_district IS NOT NULL AND assigned_taluk IS NULL) OR
        (tier = 'TalukHead' AND assigned_district IS NOT NULL AND assigned_taluk IS NOT NULL) OR
        (tier NOT IN ('DistrictHead', 'TalukHead') AND assigned_district IS NULL AND assigned_taluk IS NULL)
    )
);
CREATE UNIQUE INDEX ix_gov_admin_master_wso2_subject ON portal.gov_admin_master (wso2_subject);
