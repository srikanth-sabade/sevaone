-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.

CREATE OR REPLACE FUNCTION orchestration.sp_list_applications_with_names_for_citizen(p_citizen_id uuid)
RETURNS TABLE (
    "Id" uuid, "SevaReference" character varying(32), "DepartmentReference" text, "ServiceCode" character varying(32),
    "ServiceName" character varying(300), "DepartmentCode" character varying(64), "DepartmentName" character varying(200),
    "Status" integer, "SubmittedAt" timestamp with time zone, "SakalaDueAt" timestamp with time zone,
    "ApplicantName" text, "Address" text, "Purpose" text, "District" text, "Taluk" text
)
LANGUAGE sql STABLE AS $$
    SELECT
        a.id, a.seva_reference, a.department_reference, s.service_id, s.service_name,
        d.department_id, d.department_name, a.status, a.submitted_at, a.sakala_due_at,
        a.applicant_name, a.address, a.purpose, a.district, a.taluk
    FROM orchestration.applications a
    JOIN discovery.sevaone_service_master s ON s.id = a.scheme_id
    JOIN discovery.department_master d ON d.id = a.department_id
    WHERE a.citizen_id = p_citizen_id
    ORDER BY a.submitted_at DESC;
$$;
