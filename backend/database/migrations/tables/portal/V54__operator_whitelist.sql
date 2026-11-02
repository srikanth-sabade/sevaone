-- Roster of legitimate SevaSindhu operator IDs, imported from the department's own records
-- (not created by SevaOne). Proves "this user_id is a real, registered operator" for the
-- self-claim first-login flow (an operator enters their existing user_id, checked against this
-- table, then sets their own SevaOne/WSO2 credentials) -- it is NOT a credentials table itself;
-- there is no password/mobile/name here, only what the department roster export contained.
--
-- user_id is the operator's existing SevaSindhu identity and this table's natural key -- the
-- value operators will actually type in to claim their account. registration_id/kiosk_type(_id)
-- describe which kiosk/center the operator is attached to and are informational only; both are
-- text, not numeric, since real values include non-numeric forms (e.g. "OPR11700") alongside
-- purely numeric ones, and some rows have all three blank in the source data.
CREATE TABLE portal.operator_whitelist (
    user_id bigint NOT NULL,
    registration_id character varying(32),
    kiosk_type_id integer,
    kiosk_type character varying(20),
    -- Status code as supplied by the source roster (SPDI status) -- meaning not yet documented
    -- on SevaOne's side; carried through as-is rather than reinterpreted, so it's available
    -- once that's known. Not used to gate the self-claim flow yet.
    spdi_status smallint NOT NULL,
    imported_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT pk_operator_whitelist PRIMARY KEY (user_id)
);
