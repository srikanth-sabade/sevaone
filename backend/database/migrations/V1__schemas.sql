-- Six Postgres schemas within one database, kept separate so ownership stays clear even
-- though everything now runs as one deployable (discovery/portal/orchestration/status/audit/
-- notification -- the original multi-service design's boundaries).
CREATE SCHEMA IF NOT EXISTS discovery;
CREATE SCHEMA IF NOT EXISTS portal;
CREATE SCHEMA IF NOT EXISTS orchestration;
CREATE SCHEMA IF NOT EXISTS status;
CREATE SCHEMA IF NOT EXISTS audit;
CREATE SCHEMA IF NOT EXISTS notification;
