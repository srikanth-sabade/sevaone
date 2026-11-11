CREATE OR REPLACE FUNCTION portal.sp_list_gramaone_districts()
RETURNS TABLE (district text)
LANGUAGE sql STABLE AS $$
    SELECT DISTINCT initcap(lower(c.district)) FROM portal.gramaone_centre c WHERE c.district <> '' ORDER BY 1;
$$;

-- Centres in a district, optionally filtered by a place name matching the village, gram panchayat or taluk. Distance is NULL: there is no point to measure from.
CREATE OR REPLACE FUNCTION portal.sp_search_gramaone_centres(p_district text, p_query text, p_limit integer)
RETURNS TABLE
(
    id integer, district text, taluk text, gram_panchayat text, village text,
    latitude double precision, longitude double precision, distance_km double precision
)
LANGUAGE sql STABLE AS $$
    SELECT c.id, c.district, c.taluk, c.gram_panchayat, c.village, c.latitude, c.longitude, NULL::double precision
    FROM portal.gramaone_centre c
    WHERE upper(c.district) = upper(p_district)
      AND (coalesce(p_query, '') = ''
           OR c.village ILIKE '%' || p_query || '%' OR c.gram_panchayat ILIKE '%' || p_query || '%' OR c.taluk ILIKE '%' || p_query || '%')
    ORDER BY c.taluk, c.gram_panchayat, c.village
    LIMIT p_limit;
$$;
