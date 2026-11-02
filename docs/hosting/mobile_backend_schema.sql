-- SevaOne mobile backend: reference schema (migrations 001-015 concatenated, in apply order).
-- The backend applies these itself at startup (migrate.py). Do NOT run manually unless asked.
-- Needs: Postgres DB "sevasindhu_mobile", role "sevaone" able to CREATE SCHEMA, CREATE TABLE and use gen_random_uuid() (PG 13+).

-- ===== 001_app_schema.sql =====
-- Tables owned by the mobile app (schema `app`). The `sakala` schema is a read-only copy of the source catalogue and is never touched here.

CREATE TABLE app.citizen (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    mobile             text NOT NULL UNIQUE CHECK (mobile ~ '^[0-9]{10}$'),
    digilocker_id      text UNIQUE,
    name               text,
    name_kn            text,
    email              text,
    date_of_birth      date,
    gender             text CHECK (gender IN ('Male', 'Female', 'Other')),
    address            text,
    address_kn         text,
    district           text,
    district_kn        text,
    taluk              text,
    village            text,
    village_kn         text,
    state_name         text,
    aadhaar_masked     text,           -- 'XXXX-XXXX-1234' only: the Aadhaar number itself is never stored, nor is a hash of it
    preferred_language text NOT NULL DEFAULT 'kn' CHECK (preferred_language IN ('kn', 'en')),
    created_at         timestamptz NOT NULL DEFAULT now(),
    updated_at         timestamptz NOT NULL DEFAULT now(),
    last_login_at      timestamptz
);

-- One row per OTP sent. The OTP itself is never stored: only an HMAC keyed with a server secret, and only until it is used or expires.
CREATE TABLE app.otp_challenge (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    mobile      text NOT NULL,
    otp_hmac    text NOT NULL,
    attempts    integer NOT NULL DEFAULT 0,
    ip          text,
    created_at  timestamptz NOT NULL DEFAULT now(),
    expires_at  timestamptz NOT NULL,
    consumed_at timestamptz
);
CREATE INDEX otp_challenge_mobile_created ON app.otp_challenge (mobile, created_at);
CREATE INDEX otp_challenge_ip_created ON app.otp_challenge (ip, created_at);

-- Created after a correct OTP for a mobile that has no DigiLocker-linked citizen yet; consumed when DigiLocker sign-in completes.
CREATE TABLE app.digilocker_pending (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    mobile      text NOT NULL,
    app_param   text UNIQUE,
    created_at  timestamptz NOT NULL DEFAULT now(),
    expires_at  timestamptz NOT NULL,
    consumed_at timestamptz
);

-- Refresh tokens rotate on every use; presenting an already-used one revokes its whole family (theft detection).
CREATE TABLE app.refresh_token (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    citizen_id  uuid NOT NULL REFERENCES app.citizen (id) ON DELETE CASCADE,
    family_id   uuid NOT NULL,
    token_hash  text NOT NULL UNIQUE,   -- sha256 of the token; the token itself is never stored
    created_at  timestamptz NOT NULL DEFAULT now(),
    expires_at  timestamptz NOT NULL,
    revoked_at  timestamptz,
    replaced_by uuid
);
CREATE INDEX refresh_token_family ON app.refresh_token (family_id);

CREATE TABLE app.audit_log (
    id         bigserial PRIMARY KEY,
    citizen_id uuid,
    action     text NOT NULL,
    detail     text,
    at         timestamptz NOT NULL DEFAULT now()
);

-- ===== 002_feedback.sql =====
-- Citizen feedback submitted from the mobile app. Replaces the old /api/feedback, which wrote to the SQL Server that is unreachable from outside the office network.
CREATE TABLE app.feedback (
    id         bigserial PRIMARY KEY,
    citizen_id uuid REFERENCES app.citizen (id) ON DELETE SET NULL,   -- null for guests, and after a citizen deletes their account
    category   text NOT NULL,     -- free text: the department or topic the feedback is about
    subject    text,
    comments   text NOT NULL,
    name       text,
    mobile     text,
    email      text,
    ip         text,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX feedback_ip_created ON app.feedback (ip, created_at);
CREATE INDEX feedback_citizen_created ON app.feedback (citizen_id, created_at);

-- ===== 003_usage_event.sql =====
-- Per-citizen usage counters for rate limiting the AI assistant (protects the shared GPU server). Old rows are purged periodically.
CREATE TABLE app.usage_event (
    id         bigserial PRIMARY KEY,
    citizen_id uuid NOT NULL REFERENCES app.citizen (id) ON DELETE CASCADE,   -- deleted with the account
    kind       text NOT NULL,
    at         timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX usage_event_citizen_kind_at ON app.usage_event (citizen_id, kind, at);

-- ===== 004_applications.sql =====
-- Citizen-saved Sakala applications. The GSC number is an external reference, not a secret.
CREATE TABLE app.application (
    id          bigserial PRIMARY KEY,
    citizen_id  uuid NOT NULL REFERENCES app.citizen (id) ON DELETE CASCADE,
    gsc_number  text NOT NULL CHECK (gsc_number ~ '^[0-9]{15}$'),
    created_at  timestamptz NOT NULL DEFAULT now(),
    UNIQUE (citizen_id, gsc_number)
);
CREATE INDEX application_citizen_created ON app.application (citizen_id, created_at DESC);

-- ===== 005_favourites_recents.sql =====
-- Personal library on the account (wave 2, M8): favourite services and recently used services.
-- service_id is the catalogue service id (digits). Both tables go with the citizen (ON DELETE CASCADE), so account deletion needs no extra step.
CREATE TABLE app.favourite_service (
    citizen_id  uuid NOT NULL REFERENCES app.citizen (id) ON DELETE CASCADE,
    service_id  text NOT NULL CHECK (service_id ~ '^[0-9]{1,10}$'),
    created_at  timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (citizen_id, service_id)
);
CREATE INDEX favourite_service_citizen_created ON app.favourite_service (citizen_id, created_at DESC);

CREATE TABLE app.recent_service (
    citizen_id    uuid NOT NULL REFERENCES app.citizen (id) ON DELETE CASCADE,
    service_id    text NOT NULL CHECK (service_id ~ '^[0-9]{1,10}$'),
    last_used_at  timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (citizen_id, service_id)
);
CREATE INDEX recent_service_citizen_used ON app.recent_service (citizen_id, last_used_at DESC);

-- ===== 006_applications_v2.sql =====
-- Applications v2 (wave 2, M8): saved applications become multi-source with a stored status snapshot, so the list is fast, works when the
-- source is slow, and ServicePlus applications can be tied to the citizen later (source = 'serviceplus'). The old five-item cap was code only.
ALTER TABLE app.application
    ADD COLUMN source           text NOT NULL DEFAULT 'sakala' CHECK (source IN ('sakala', 'serviceplus')),
    ADD COLUMN external_ref     text,
    ADD COLUMN service_name     text,
    ADD COLUMN status           text,
    ADD COLUMN status_snapshot  jsonb,
    ADD COLUMN last_checked_at  timestamptz,
    ADD COLUMN updated_at       timestamptz NOT NULL DEFAULT now(),
    ADD COLUMN nickname         text;

UPDATE app.application SET external_ref = gsc_number WHERE external_ref IS NULL;
ALTER TABLE app.application ALTER COLUMN external_ref SET NOT NULL;

-- gsc_number is Sakala-only from now on; the (source, external_ref) pair identifies an application for any source.
ALTER TABLE app.application ALTER COLUMN gsc_number DROP NOT NULL;
ALTER TABLE app.application DROP CONSTRAINT IF EXISTS application_gsc_number_check;
ALTER TABLE app.application ADD CONSTRAINT application_gsc_number_check CHECK (gsc_number IS NULL OR gsc_number ~ '^[0-9]{15}$');
ALTER TABLE app.application ADD CONSTRAINT application_sakala_ref_check CHECK (source <> 'sakala' OR external_ref ~ '^[0-9]{15}$');
ALTER TABLE app.application DROP CONSTRAINT IF EXISTS application_citizen_id_gsc_number_key;
ALTER TABLE app.application ADD CONSTRAINT application_citizen_source_ref_key UNIQUE (citizen_id, source, external_ref);

-- ===== 007_service_centre.sql =====
-- Nearby service centres (wave 2, M9). Sample data only for now (is_sample = true); real rows are seeded later once a department or CeG
-- gives a dataset. Not tied to any citizen: nothing here needs to be removed on account deletion.
CREATE TABLE app.service_centre (
    id          bigserial PRIMARY KEY,
    code        text NOT NULL UNIQUE,
    type        text NOT NULL CHECK (type IN ('csc', 'sakala_kendra', 'nadakacheri', 'taluk_office', 'gram_panchayat', 'other')),
    name        text NOT NULL,
    name_kn     text,
    address     text NOT NULL,
    address_kn  text,
    district    text NOT NULL,
    district_kn text,
    taluk       text,
    pincode     text,
    lat         double precision NOT NULL CHECK (lat BETWEEN -90 AND 90),
    lng         double precision NOT NULL CHECK (lng BETWEEN -180 AND 180),
    phone       text,
    hours       text,
    services    text[] NOT NULL DEFAULT '{}',
    is_sample   boolean NOT NULL DEFAULT false,
    active      boolean NOT NULL DEFAULT true,
    created_at  timestamptz NOT NULL DEFAULT now(),
    updated_at  timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX service_centre_district_taluk ON app.service_centre (district, taluk) WHERE active;
CREATE INDEX service_centre_type ON app.service_centre (type) WHERE active;
CREATE INDEX service_centre_lat_lng ON app.service_centre (lat, lng) WHERE active;

-- ===== 008_notifications.sql =====
-- In-app notification inbox and device tokens for push (wave 2, M10). Both languages are stored so the app can pick, the same
-- convention as everything else here. Push delivery itself is a separate adapter (push_sender.py); this table is the source of
-- truth the app reads regardless of whether a push was ever sent. Both tables go with the citizen (ON DELETE CASCADE).
CREATE TABLE app.device_token (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    citizen_id    uuid NOT NULL REFERENCES app.citizen (id) ON DELETE CASCADE,
    token         text NOT NULL UNIQUE,
    provider      text NOT NULL CHECK (provider IN ('fcm', 'apns')),
    platform      text NOT NULL CHECK (platform IN ('android', 'ios')),
    app_version   text,
    locale        text,
    created_at    timestamptz NOT NULL DEFAULT now(),
    last_seen_at  timestamptz NOT NULL DEFAULT now(),
    disabled_at   timestamptz
);
CREATE INDEX device_token_citizen ON app.device_token (citizen_id) WHERE disabled_at IS NULL;

CREATE TABLE app.notification (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    citizen_id    uuid NOT NULL REFERENCES app.citizen (id) ON DELETE CASCADE,
    kind          text NOT NULL CHECK (kind IN ('application_status', 'grievance_status', 'document_ready', 'system', 'test')),
    title_en      text NOT NULL,
    title_kn      text NOT NULL,
    body_en       text NOT NULL,
    body_kn       text NOT NULL,
    route         text CHECK (route IN ('applications', 'grievance', 'document', 'notifications')),
    route_id      text,
    -- Set only by an automatic trigger (an application/grievance/document event), never by /notifications/test or the admin endpoint,
    -- so the same event is never shown twice: a partial unique index lets many rows share a NULL dedupe_key.
    dedupe_key    text,
    data          jsonb NOT NULL DEFAULT '{}',
    created_at    timestamptz NOT NULL DEFAULT now(),
    read_at       timestamptz,
    push_status   text NOT NULL DEFAULT 'none' CHECK (push_status IN ('none', 'queued', 'sent', 'failed')),
    push_sent_at  timestamptz
);
CREATE INDEX notification_citizen_created ON app.notification (citizen_id, created_at DESC);
CREATE INDEX notification_citizen_unread ON app.notification (citizen_id) WHERE read_at IS NULL;
CREATE UNIQUE INDEX notification_citizen_dedupe ON app.notification (citizen_id, dedupe_key) WHERE dedupe_key IS NOT NULL;

CREATE TABLE app.notification_pref (
    citizen_id   uuid PRIMARY KEY REFERENCES app.citizen (id) ON DELETE CASCADE,
    push_enabled boolean NOT NULL DEFAULT true,
    muted_kinds  text[] NOT NULL DEFAULT '{}',
    updated_at   timestamptz NOT NULL DEFAULT now()
);

-- ===== 009_documents.sql =====
-- DigiLocker document content, cached (wave 2, M11). `doc_key` is an opaque id the client is given (HMAC of citizen_id+DocURI): the
-- server always holds the real DigiLocker URI, so the app can never ask for an arbitrary one. `document_file` is a short-lived cache of
-- the actual bytes (stored in object storage, see storage.py); the row here only tracks metadata and where the bytes are.
CREATE TABLE app.digilocker_document (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    citizen_id    uuid NOT NULL REFERENCES app.citizen (id) ON DELETE CASCADE,
    doc_key       text NOT NULL,
    doc_uri       text NOT NULL,
    name          text,
    doc_type      text,
    issuer        text,
    reference_no  text,
    formats       text[] NOT NULL DEFAULT '{}',
    is_sample     boolean NOT NULL DEFAULT false,
    listed_at     timestamptz NOT NULL DEFAULT now(),
    UNIQUE (citizen_id, doc_key)
);

CREATE TABLE app.document_file (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id       uuid NOT NULL REFERENCES app.digilocker_document (id) ON DELETE CASCADE,
    format            text NOT NULL CHECK (format IN ('pdf', 'xml', 'json')),
    mime              text NOT NULL,
    size_bytes        integer NOT NULL,
    sha256            text NOT NULL,
    storage_key       text NOT NULL,
    source            text NOT NULL CHECK (source IN ('live', 'stub')),
    fetched_at        timestamptz NOT NULL DEFAULT now(),
    expires_at        timestamptz NOT NULL,
    last_accessed_at  timestamptz NOT NULL DEFAULT now(),
    UNIQUE (document_id, format)
);
CREATE INDEX document_file_expires ON app.document_file (expires_at);

-- A retry outbox for storage cleanup that failed at the moment of account deletion (the citizen row is already gone by then, so this
-- has no foreign key): scripts/sweep_storage_purge.py retries these until they succeed, then marks them done.
CREATE TABLE app.storage_purge (
    id          bigserial PRIMARY KEY,
    prefix      text NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),
    done_at     timestamptz
);

-- ===== 010_grievances.sql =====
-- Grievances (wave 2, M12). Stored and shown to the citizen immediately; forwarding to the external authority (CPGRAMS or a
-- Karnataka system, undecided) is a separate step behind grievance_gateway.py, so `forwarded` starts false and the app says so
-- honestly. `idempotency_key` makes a double-tapped submit create exactly one row. All three tables go with the citizen
-- (ON DELETE CASCADE via grievance -> citizen, and attachment/history -> grievance).
CREATE TABLE app.grievance (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    reference          text NOT NULL UNIQUE,  -- "GRV-YYYYMMDD-XXXXXX", shown to the citizen; never the external authority's own number
    citizen_id         uuid NOT NULL REFERENCES app.citizen (id) ON DELETE CASCADE,
    idempotency_key    text NOT NULL,
    category           text NOT NULL CHECK (category IN ('service_delay', 'wrongly_rejected', 'bribe_demand', 'staff_conduct', 'technical_issue', 'document_issue', 'other')),
    service_id         text,
    application_id     bigint REFERENCES app.application (id) ON DELETE SET NULL,
    department         text,
    subject            text NOT NULL CHECK (char_length(subject) BETWEEN 5 AND 120),
    description        text NOT NULL CHECK (char_length(description) BETWEEN 20 AND 2000),
    incident_date      date,
    district           text,
    taluk              text,
    contact_name       text NOT NULL,
    contact_mobile     text NOT NULL,  -- a snapshot of the citizen's mobile at submission time, so it survives later profile edits
    consent_shared     boolean NOT NULL,
    status             text NOT NULL DEFAULT 'received' CHECK (status IN ('received', 'forwarded', 'in_progress', 'resolved', 'closed', 'withdrawn', 'rejected')),
    external_system    text,
    external_ref       text,
    external_status    text,
    external_synced_at timestamptz,
    created_at         timestamptz NOT NULL DEFAULT now(),
    updated_at         timestamptz NOT NULL DEFAULT now(),
    UNIQUE (citizen_id, idempotency_key)
);
CREATE INDEX grievance_citizen_created ON app.grievance (citizen_id, created_at DESC);
CREATE INDEX grievance_external ON app.grievance (external_system, external_ref) WHERE external_system IS NOT NULL;

CREATE TABLE app.grievance_attachment (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    grievance_id uuid NOT NULL REFERENCES app.grievance (id) ON DELETE CASCADE,
    filename    text NOT NULL,
    mime        text NOT NULL,
    size_bytes  integer NOT NULL,
    sha256      text NOT NULL,
    storage_key text NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX grievance_attachment_grievance ON app.grievance_attachment (grievance_id);

CREATE TABLE app.grievance_status_history (
    id           bigserial PRIMARY KEY,
    grievance_id uuid NOT NULL REFERENCES app.grievance (id) ON DELETE CASCADE,
    status       text NOT NULL,
    note         text,
    actor        text NOT NULL CHECK (actor IN ('citizen', 'system', 'officer')),
    at           timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX grievance_status_history_grievance ON app.grievance_status_history (grievance_id, at);

-- ===== 011_serviceplus_auth.sql =====
-- ServicePlus-first mobile authentication. The provider is centrally configured and never exposed as a user choice.
CREATE TABLE app.auth_provider_config (
    id          smallint PRIMARY KEY CHECK (id = 1),
    provider    text NOT NULL CHECK (provider IN ('serviceplus', 'sevaone')),
    updated_at  timestamptz NOT NULL DEFAULT now()
);

INSERT INTO app.auth_provider_config (id, provider)
VALUES (1, 'serviceplus')
ON CONFLICT (id) DO NOTHING;

-- ServicePlus credentials and opaque values belong to the backend only. Passwords are one-way hashes. The three token
-- columns are reserved for the NIC-approved adapter and must contain encrypted values, never usable plaintext.
CREATE TABLE app.serviceplus_account (
    id                          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    citizen_id                  uuid NOT NULL UNIQUE REFERENCES app.citizen (id) ON DELETE CASCADE,
    login_name                  text NOT NULL,
    password_hash               text NOT NULL,
    provider_subject            text,
    device_token_ciphertext     text,
    user_token_ciphertext       text,
    sign_no_ciphertext          text,
    status                      text NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'disabled')),
    last_verified_at            timestamptz,
    token_expires_at            timestamptz,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    updated_at                  timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX serviceplus_account_login_name_lower ON app.serviceplus_account (lower(login_name));

-- ===== 012_serviceplus_profile.sql =====
-- Non-secret display metadata returned by ServicePlus validateUser. Credentials and provider tokens remain backend-only.
ALTER TABLE app.serviceplus_account
    ADD COLUMN IF NOT EXISTS provider_display_name text,
    ADD COLUMN IF NOT EXISTS provider_role text,
    ADD COLUMN IF NOT EXISTS provider_state_code integer,
    ADD COLUMN IF NOT EXISTS provider_state_name text;

-- ===== 013_serviceplus_catalog.sql =====
-- Full ServicePlus live catalogue (getServiceJson), ingested server-side and stored so the app can browse/search
-- it without every citizen needing a live ServicePlus session just to look at the list. Populated by
-- scripts/ingest_serviceplus_catalog.py, which calls getServiceJson once (using any one citizen's live session --
-- the catalogue itself is state-wide, not personal) and upserts every row here. Not tied to any citizen: nothing
-- here is removed on account deletion. `department_id` is the same slug scheme mobile_catalog.py already uses,
-- so a department here never collides with the (now-retired) sakala-sourced one.
CREATE TABLE app.serviceplus_service (
    service_id      text PRIMARY KEY,
    name            text NOT NULL,
    description     text,
    department_id   text NOT NULL,
    department_name text NOT NULL,
    state_name      text,
    state_code      int,
    is_featured     boolean NOT NULL DEFAULT false,
    featured_rank   int,
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX serviceplus_service_department ON app.serviceplus_service (department_id);
CREATE INDEX serviceplus_service_featured ON app.serviceplus_service (featured_rank) WHERE is_featured;
-- No full-text/trigram index: ~1100 rows total, a plain ILIKE scan on every search is fast enough.

-- Departments get their own small table too (rather than deriving department rows purely from a GROUP BY over
-- services every request) so a department can be marked "featured" independently of any one service inside it,
-- and so the department's display name/count don't need recomputing from scratch on every list call.
CREATE TABLE app.serviceplus_department (
    department_id   text PRIMARY KEY,
    name            text NOT NULL,
    service_count   int NOT NULL DEFAULT 0,
    is_featured     boolean NOT NULL DEFAULT false,
    featured_rank   int,
    updated_at      timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX serviceplus_department_featured ON app.serviceplus_department (featured_rank) WHERE is_featured;

-- ===== 014_digilocker_link.sql =====
-- A DigiLocker connection started by an already signed-in ServicePlus citizen.
-- Separate from the legacy OTP first-sign-in pending table so a callback can only
-- link the exact JWT citizen that initiated it.
CREATE TABLE app.digilocker_link_pending (
    id          uuid PRIMARY KEY,
    citizen_id  uuid NOT NULL REFERENCES app.citizen (id) ON DELETE CASCADE,
    app_param   text NOT NULL UNIQUE,
    created_at  timestamptz NOT NULL DEFAULT now(),
    expires_at  timestamptz NOT NULL,
    consumed_at timestamptz
);
CREATE INDEX digilocker_link_pending_citizen ON app.digilocker_link_pending (citizen_id, created_at DESC);

-- ===== 015_digilocker_device_key.sql =====
-- Per-phone DigiLocker key: issued once when the citizen approves DigiLocker on that phone, so later visits fetch
-- documents without any SevaOne/ServicePlus login. Only a SHA-256 hash of the key is stored.
CREATE TABLE app.digilocker_device_key (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    citizen_id   uuid NOT NULL REFERENCES app.citizen (id) ON DELETE CASCADE,
    key_hash     text NOT NULL UNIQUE,
    device_label text,
    created_at   timestamptz NOT NULL DEFAULT now(),
    last_used_at timestamptz,
    revoked_at   timestamptz
);
CREATE INDEX digilocker_device_key_citizen ON app.digilocker_device_key (citizen_id);

-- A key-based link starts before any citizen is known; the citizen is resolved from the DigiLocker account on completion.
ALTER TABLE app.digilocker_link_pending ALTER COLUMN citizen_id DROP NOT NULL;

-- CeG does not always return a mobile number, and a DigiLocker-only citizen has no other source for one.
ALTER TABLE app.citizen ALTER COLUMN mobile DROP NOT NULL;

-- Anonymous assistant use is rate-limited by a client key (the caller's address) instead of a citizen.
ALTER TABLE app.usage_event ALTER COLUMN citizen_id DROP NOT NULL;
ALTER TABLE app.usage_event ADD COLUMN IF NOT EXISTS client_key text;
CREATE INDEX IF NOT EXISTS usage_event_client_kind_at ON app.usage_event (client_key, kind, at);
