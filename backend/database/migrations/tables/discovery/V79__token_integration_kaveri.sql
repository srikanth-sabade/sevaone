-- Second integration type: Kaveri 2.0 (single sign-on handoff). Unlike Seva Sindhu, the department's addresses and
-- the platform's technical login are the same for every service, so they are kept once per type in
-- token_integration_provider (settings as JSON, the password encrypted), not repeated on each service.
-- Each service only adds its Kaveri service id (token_integration.extra_config -> "kaveri").

CREATE TABLE discovery.token_integration_provider (
    type_key varchar(64) NOT NULL,
    settings jsonb NOT NULL DEFAULT '{}'::jsonb,
    secret_ciphertext text,
    updated_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_by text NOT NULL DEFAULT 'system',
    CONSTRAINT pk_token_integration_provider PRIMARY KEY (type_key),
    CONSTRAINT fk_token_integration_provider_type FOREIGN KEY (type_key) REFERENCES discovery.token_integration_type (type_key)
);

INSERT INTO discovery.token_integration_type (type_key, display_name, description, fields_json) VALUES (
    'kaveri_sso',
    'Single sign-on (Kaveri 2.0)',
    'SevaOne signs in to Kaveri with its own technical login, asks Kaveri for a single sign-on address for the citizen (sending their profile, our service id and our application number), and sends the citizen there. The status API is asked for progress.',
    '{"edcsServiceId":{"required":true,"label":"Kaveri service id","help":"The id of the Kaveri service this SevaOne service stands for."},
      "provider":{"fields":["loginUrl","refreshUrl","ssoUrl","statusUrl","username","password"]}}'::jsonb
);

-- A single-sign-on service has no fixed landing page: the address comes back from Kaveri for each citizen.
ALTER TABLE discovery.token_integration DROP CONSTRAINT ck_token_integration_enabled_needs_url;
