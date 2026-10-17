-- The nomination timestamp already existed (nodal_officer_nomination_requested_at) but nothing
-- recorded who was actually nominated once the request was fulfilled.
ALTER TABLE discovery.department_onboarding ADD COLUMN nodal_officer_name character varying(200);
