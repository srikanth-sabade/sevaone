CREATE TABLE portal.sevaone_user (
    id uuid NOT NULL,
    sevaone_user_id bigint GENERATED ALWAYS AS IDENTITY,
    wso2_subject character varying(200) NOT NULL,
    username character varying(200) NOT NULL,
    name character varying(200) NOT NULL,
    mobile character varying(15),
    mobile_verified_at timestamp with time zone,
    email character varying(200),
    address character varying(500),
    district character varying(120),
    taluk character varying(120),
    state_name character varying(100),
    village_name character varying(100),
    date_of_birth date,
    gender character varying(20),
    aadhaar_masked character varying(20),
    aadhaar_hash character varying(256),
    seva_id character varying(32) NOT NULL,
    profile_completed boolean NOT NULL DEFAULT FALSE,
    profile_finish_pct integer NOT NULL DEFAULT 0,
    created_by_operator_id character varying(50),
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_sevaone_user PRIMARY KEY (id),
    CONSTRAINT ck_sevaone_user_gender CHECK (gender IS NULL OR gender IN ('Male', 'Female', 'Other', 'PreferNotToSay')),
    CONSTRAINT ck_sevaone_user_mobile_format CHECK (mobile IS NULL OR mobile ~ '^[0-9]{10,15}$'),
    CONSTRAINT ck_sevaone_user_profile_finish_pct_range CHECK (profile_finish_pct BETWEEN 0 AND 100)
);
-- Unique where present: an OTP-verified mobile, or a real DigiLocker Aadhaar, can only ever
-- belong to one account -- see CitizenSessionService.EstablishAsync's duplicate-account checks.
CREATE UNIQUE INDEX ix_sevaone_user_aadhaar_hash ON portal.sevaone_user (aadhaar_hash) WHERE aadhaar_hash IS NOT NULL;
CREATE UNIQUE INDEX ix_sevaone_user_mobile ON portal.sevaone_user (mobile) WHERE mobile_verified_at IS NOT NULL;
CREATE INDEX ix_sevaone_user_seva_id ON portal.sevaone_user (seva_id);
CREATE UNIQUE INDEX ix_sevaone_user_sevaone_user_id ON portal.sevaone_user (sevaone_user_id);
CREATE UNIQUE INDEX ix_sevaone_user_wso2subject ON portal.sevaone_user (wso2_subject);
