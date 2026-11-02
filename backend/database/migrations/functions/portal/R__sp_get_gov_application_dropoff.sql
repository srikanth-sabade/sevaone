CREATE OR REPLACE FUNCTION portal.sp_get_gov_application_dropoff(
    p_district text DEFAULT NULL,
    p_taluk text DEFAULT NULL,
    p_department_code text DEFAULT NULL)
RETURNS TABLE (status integer, open_count bigint, avg_time_in_status_hours numeric)
LANGUAGE sql STABLE AS $$
    SELECT a.status,
           count(*) AS open_count,
           round(avg(EXTRACT(EPOCH FROM (now() - COALESCE(
               (SELECT max(e.at) FROM status.application_events e
                WHERE e.application_id = a.id AND e.status = a.status),
               a.submitted_at))) / 3600)::numeric, 2) AS avg_time_in_status_hours
    FROM orchestration.applications a
    WHERE a.status IN (0, 1, 2)
      AND (p_district IS NULL OR lower(trim(a.district)) = lower(trim(p_district)))
      AND (p_taluk IS NULL OR lower(trim(a.taluk)) = lower(trim(p_taluk)))
      AND (p_department_code IS NULL OR a.department_id = (
          SELECT d.id FROM discovery.department_master d WHERE d.department_id = p_department_code))
    GROUP BY a.status
    ORDER BY a.status;
$$;
