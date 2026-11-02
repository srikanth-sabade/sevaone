-- Token integration ("Lane 2"): the admin-configurable, citizen-facing handoff to a department
-- portal. Deliberately separate from the operator-only Sakala setup (sevaone_service_master.
-- integration_method / sevasindhu_service_id -> 'ajsk', 'sakala_token', 'sakala_token_escom'),
-- which is left exactly as it is. Nothing here reads or writes those columns.
--
-- token_integration_type is the registry of integration kinds. A new kind is one new row (plus
-- the apply engine learning to run it); the admin screen reads fields_json to know what to ask.
-- token_integration holds one setup per service.

CREATE TABLE discovery.token_integration_type (
    type_key varchar(64) NOT NULL,
    display_name varchar(200) NOT NULL,
    description text,
    fields_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_token_integration_type PRIMARY KEY (type_key)
);

INSERT INTO discovery.token_integration_type (type_key, display_name, description, fields_json) VALUES (
    'seva_sindhu_v3',
    'Token-based integration (Seva Sindhu V3)',
    'The user is sent to the department portal with a form POST carrying UserId, edcsApplicationRefNo, Token, assistedmode and ServiceId (plus mobile / digilockerid when the department needs them). The department later calls our acknowledgement API, and we can ask its status API for a GSC number.',
    '{"landingUrl":{"required":true,"label":"Department URL (URL 1)","help":"Page that receives the POST form. Must answer HTTP 200."},
      "sendMobile":{"label":"Also send mobile","help":"Only for departments that ask for it."},
      "sendDigilockerId":{"label":"Also send DigiLocker id","help":"Only for departments that ask for it."},
      "statusUrl":{"label":"Status check API (URL 3)","help":"Department API that takes {\"GscNumber\": \"...\"}."},
      "allowedIps":{"label":"Department server IPs","help":"IPs the department calls us from (acknowledgement API allow-list)."}}'::jsonb
);

CREATE TABLE discovery.token_integration (
    id uuid NOT NULL,
    service_id uuid NOT NULL,
    type_key varchar(64) NOT NULL,
    landing_url varchar(500),
    send_mobile boolean NOT NULL DEFAULT false,
    send_digilocker_id boolean NOT NULL DEFAULT false,
    status_url varchar(500),
    allowed_ips text[] NOT NULL DEFAULT '{}',
    extra_config jsonb NOT NULL DEFAULT '{}'::jsonb,
    is_enabled boolean NOT NULL DEFAULT false,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_by text NOT NULL DEFAULT 'system',
    version integer NOT NULL DEFAULT 1,
    CONSTRAINT pk_token_integration PRIMARY KEY (id),
    CONSTRAINT uq_token_integration_service UNIQUE (service_id),
    CONSTRAINT fk_token_integration_service FOREIGN KEY (service_id) REFERENCES discovery.sevaone_service_master (id) ON DELETE CASCADE,
    CONSTRAINT fk_token_integration_type FOREIGN KEY (type_key) REFERENCES discovery.token_integration_type (type_key),
    -- backstop for the service-layer rule: a setup can't be switched on without somewhere to send the user
    CONSTRAINT ck_token_integration_enabled_needs_url CHECK (is_enabled = false OR (landing_url IS NOT NULL AND landing_url <> ''))
);
CREATE INDEX ix_token_integration_type ON discovery.token_integration (type_key);
