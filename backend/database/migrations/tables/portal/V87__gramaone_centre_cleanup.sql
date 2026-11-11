-- The source list has repeated rows (same operator at the same spot) and a few centres whose coordinates are plainly wrong
-- (e.g. a Belagavi village plotted in Bengaluru). Drop repeats, then any centre more than 120 km from the median position of its own district.
DELETE FROM portal.gramaone_centre a
USING portal.gramaone_centre b
WHERE a.opr_code = b.opr_code
  AND round(a.latitude::numeric, 4) = round(b.latitude::numeric, 4)
  AND round(a.longitude::numeric, 4) = round(b.longitude::numeric, 4)
  AND a.id > b.id;

WITH m AS (
    SELECT upper(district) AS d,
           percentile_cont(0.5) WITHIN GROUP (ORDER BY latitude)  AS la,
           percentile_cont(0.5) WITHIN GROUP (ORDER BY longitude) AS lo
    FROM portal.gramaone_centre GROUP BY 1
)
DELETE FROM portal.gramaone_centre c
USING m
WHERE m.d = upper(c.district)
  AND 2 * 6371 * asin(sqrt(
        power(sin(radians(c.latitude - m.la) / 2), 2) +
        cos(radians(m.la)) * cos(radians(c.latitude)) * power(sin(radians(c.longitude - m.lo) / 2), 2))) > 120;
