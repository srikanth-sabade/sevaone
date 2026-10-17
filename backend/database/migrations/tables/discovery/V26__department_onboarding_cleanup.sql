-- Drop api_doc_v1_shared_at/api_doc_v2_shared_at (V21) -- speculative fields with no reader,
-- carried straight over from a source tracking sheet without a real use for them here.
ALTER TABLE discovery.department_onboarding DROP COLUMN api_doc_v1_shared_at;
ALTER TABLE discovery.department_onboarding DROP COLUMN api_doc_v2_shared_at;

-- api_implemented and redirection_enabled were two independent booleans for what's actually a
-- linear progression (redirection can't sensibly be live before the API is implemented), and
-- api_implemented's name collided in meaning with department_master.is_integrated (a different,
-- older integration mechanism). One ordered status column replaces both problems at once.
ALTER TABLE discovery.department_onboarding DROP COLUMN api_implemented;
ALTER TABLE discovery.department_onboarding DROP COLUMN redirection_enabled;
ALTER TABLE discovery.department_onboarding ADD COLUMN onboarding_stage character varying(30) NOT NULL DEFAULT 'NotStarted';
ALTER TABLE discovery.department_onboarding ADD CONSTRAINT ck_department_onboarding_stage
    CHECK (onboarding_stage IN ('NotStarted', 'ApiImplemented', 'Live'));

-- Every other table in this schema has created_at; this one only had updated_at.
ALTER TABLE discovery.department_onboarding ADD COLUMN created_at timestamp with time zone NOT NULL DEFAULT now();
