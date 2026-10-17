-- sp_get_audit_logs_by_citizen filters on citizen_id and sorts by at DESC -- the existing
-- ix_audit_logs_at index (on at alone) doesn't serve that filter, so this query was a full
-- sequential scan of the whole table (everyone's history, not just the one citizen's) on every
-- "my activity" page load. This composite index matches the actual query shape.
CREATE INDEX ix_audit_logs_citizen_id_at ON audit.audit_logs (citizen_id, at DESC);
