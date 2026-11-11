-- Great-circle distance (haversine, 6371 km); a full scan of ~8.8k rows is fast enough that no spatial index is needed.
CREATE OR REPLACE FUNCTION portal.sp_nearest_gramaone_centres(p_lat double precision, p_lon double precision, p_limit integer)
RETURNS TABLE
(
    id integer, district text, taluk text, gram_panchayat text, village text,
    latitude double precision, longitude double precision, distance_km double precision
)
LANGUAGE sql STABLE AS $$
    SELECT c.id, c.district, c.taluk, c.gram_panchayat, c.village, c.latitude, c.longitude,
           2 * 6371 * asin(sqrt(
               power(sin(radians(c.latitude - p_lat) / 2), 2) +
               cos(radians(p_lat)) * cos(radians(c.latitude)) * power(sin(radians(c.longitude - p_lon) / 2), 2))) AS distance_km
    FROM portal.gramaone_centre c
    ORDER BY distance_km
    LIMIT p_limit;
$$;
