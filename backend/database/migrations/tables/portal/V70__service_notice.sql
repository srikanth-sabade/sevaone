CREATE TABLE IF NOT EXISTS portal.service_notice
(
    id smallint PRIMARY KEY DEFAULT 1 CHECK (id = 1),
    title character varying(120) NOT NULL,
    message character varying(500) NOT NULL,
    severity character varying(16) NOT NULL CHECK (severity IN ('info', 'warning', 'critical')),
    is_active boolean NOT NULL DEFAULT false,
    starts_at timestamp with time zone,
    ends_at timestamp with time zone,
    updated_by uuid,
    updated_at timestamp with time zone NOT NULL DEFAULT now(),
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT ck_service_notice_schedule CHECK (ends_at IS NULL OR starts_at IS NULL OR ends_at > starts_at)
);
