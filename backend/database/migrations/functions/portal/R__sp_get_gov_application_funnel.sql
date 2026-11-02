CREATE OR REPLACE FUNCTION portal.sp_get_gov_application_funnel(p_district text DEFAULT NULL, p_taluk text DEFAULT NULL)
RETURNS TABLE (
    registered_citizens bigint, profile_completed_citizens bigint,
    citizens_submitted bigint, citizens_completed bigint)
LANGUAGE sql STABLE AS $$
    SELECT
        (SELECT count(*) FROM portal.sevaone_user u
         WHERE (p_district IS NULL OR lower(trim(u.district)) = lower(trim(p_district)))
           AND (p_taluk IS NULL OR lower(trim(u.taluk)) = lower(trim(p_taluk)))) AS registered_citizens,
        (SELECT count(*) FROM portal.sevaone_user u
         WHERE u.profile_completed IS TRUE
           AND (p_district IS NULL OR lower(trim(u.district)) = lower(trim(p_district)))
           AND (p_taluk IS NULL OR lower(trim(u.taluk)) = lower(trim(p_taluk)))) AS profile_completed_citizens,
        (SELECT count(DISTINCT a.citizen_id) FROM orchestration.applications a
         WHERE (p_district IS NULL OR lower(trim(a.district)) = lower(trim(p_district)))
           AND (p_taluk IS NULL OR lower(trim(a.taluk)) = lower(trim(p_taluk)))) AS citizens_submitted,
        (SELECT count(DISTINCT a.citizen_id) FROM orchestration.applications a
         WHERE a.status = 3
           AND (p_district IS NULL OR lower(trim(a.district)) = lower(trim(p_district)))
           AND (p_taluk IS NULL OR lower(trim(a.taluk)) = lower(trim(p_taluk)))) AS citizens_completed;
$$;
