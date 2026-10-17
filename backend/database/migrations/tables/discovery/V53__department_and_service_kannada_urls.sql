-- The xlsx source's "Dept home page url english"/"Dept home page url kannada" columns are
-- filled per SERVICE ROW (not just once per department -- Revenue's AJSK sub-department and
-- its main services genuinely point at two different portals, same for Food & Civil
-- Supplies' two sub-portals), and well-populated (every real department sheet except
-- Education has both columns filled for every row). The ingestion pipeline was only reading
-- "Deep Link URL" for a service's apply link -- a column that is 100% empty in the real
-- data -- so no service ever got an Apply URL. deeplink_citizen_url (English) already
-- existed and is wired end to end; this adds its Kannada counterpart.
ALTER TABLE discovery.sevaone_service_master ADD COLUMN deeplink_citizen_url_kannada character varying(500);

-- department_url (English) already exists; this adds the Kannada counterpart for the
-- department page's own "visit department portal" link.
ALTER TABLE discovery.department_master ADD COLUMN department_url_kannada text;
