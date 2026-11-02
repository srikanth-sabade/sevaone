-- For the "token from Seva Sindhu" type the department reports its acknowledgement to Seva Sindhu's API, not ours, so
-- SevaOne asks for it. ack_checked_at: when the background job last looked for this application's acknowledgement.
ALTER TABLE discovery.token_integration_attempt ADD COLUMN ack_checked_at timestamp with time zone;
