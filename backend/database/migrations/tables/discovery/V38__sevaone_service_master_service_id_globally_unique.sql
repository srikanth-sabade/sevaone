-- service_id was only unique per-department (department_id, service_id), but
-- GetSchemeByServiceCodeAsync -- the citizen-facing apply flow, search, and the JWS token's
-- service_id claim -- looks a scheme up by service_id ALONE, with no department in hand at that
-- point. Two departments reusing the same code would silently misroute that lookup; nothing
-- caught it at the DB level. Making service_id unique on its own closes that gap, and makes the
-- old compound index redundant (a unique service_id trivially makes (department_id, service_id)
-- unique too), so it's dropped in the same migration rather than left alongside a stricter one.
DROP INDEX discovery.ix_sevaone_service_master_department_id_service_code;
CREATE UNIQUE INDEX ix_sevaone_service_master_service_id ON discovery.sevaone_service_master (service_id);
