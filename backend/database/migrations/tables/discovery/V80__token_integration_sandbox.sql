-- Sandbox: a department can try the whole token integration (token check, acknowledgement) without touching any citizen's
-- dashboard. Sandbox applications use the seeded test citizen, carry the reference prefix EDCSTEST, are never checked by the
-- status poller and never create an application record.
-- token_verified_at: when the department first checked the token (a later check is flagged as a repeat).

ALTER TABLE discovery.token_integration_attempt
    ADD COLUMN is_sandbox boolean NOT NULL DEFAULT false,
    ADD COLUMN token_verified_at timestamp with time zone;
