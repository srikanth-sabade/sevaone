-- The source list spells districts many ways (Bagalkot/Bagalkote, Tumakuru/Tumkuru, "Dakshina0Kannada", taluk names in the district column ...).
-- Fold every variant onto the 31 current district names, tidy the other name columns, then re-run the position outlier check on the clean districts.
UPDATE portal.gramaone_centre SET district = CASE upper(trim(district))
    WHEN 'ALAND' THEN 'Kalaburagi'
    WHEN 'BAGALKOT' THEN 'Bagalkote'
    WHEN 'BANGALORE URBAN' THEN 'Bengaluru Urban'
    WHEN 'BENGALURU' THEN 'Bengaluru Urban'
    WHEN 'BENGALURU SOUTH' THEN 'Ramanagara'
    WHEN 'BELAGAVI-1' THEN 'Belagavi'
    WHEN 'BELAGAVI-2' THEN 'Belagavi'
    WHEN 'CHAMARAJANAGARA' THEN 'Chamarajanagar'
    WHEN 'CHAMARAJNAGAR' THEN 'Chamarajanagar'
    WHEN 'CHICKBALLAPUR' THEN 'Chikkaballapura'
    WHEN 'CHIKAMANGALU' THEN 'Chikkamagaluru'
    WHEN 'DAKSHINA0KANNADA' THEN 'Dakshina Kannada'
    WHEN 'DAVANGERE' THEN 'Davanagere'
    WHEN 'KALABURAGI (GULBARGA)' THEN 'Kalaburagi'
    WHEN 'KALABURGI' THEN 'Kalaburagi'
    WHEN 'KALBURGI' THEN 'Kalaburagi'
    WHEN 'TUMKURU' THEN 'Tumakuru'
    WHEN 'UTTARAKANNADA' THEN 'Uttara Kannada'
    WHEN 'VIJAYANAGAR' THEN 'Vijayanagara'
    WHEN 'VIJAYAPUR' THEN 'Vijayapura'
    WHEN 'VIJAYAPURA (BIJAPUR)' THEN 'Vijayapura'
    WHEN 'YADAGIRI' THEN 'Yadgir'
    WHEN 'YADGIRI' THEN 'Yadgir'
    ELSE initcap(lower(trim(district)))
END;

-- A blank district takes the district of the nearest centre that has one.
UPDATE portal.gramaone_centre c SET district = (
    SELECT n.district FROM portal.gramaone_centre n WHERE n.district <> ''
    ORDER BY power(n.latitude - c.latitude, 2) + power(n.longitude - c.longitude, 2) LIMIT 1)
WHERE c.district = '';

UPDATE portal.gramaone_centre SET
    taluk = CASE WHEN trim(taluk) ~ '^[0-9]+$' THEN '' ELSE initcap(lower(trim(taluk))) END,
    gram_panchayat = initcap(lower(trim(gram_panchayat))),
    village = initcap(lower(trim(village)));

WITH m AS (
    SELECT district AS d,
           percentile_cont(0.5) WITHIN GROUP (ORDER BY latitude)  AS la,
           percentile_cont(0.5) WITHIN GROUP (ORDER BY longitude) AS lo
    FROM portal.gramaone_centre GROUP BY 1
)
DELETE FROM portal.gramaone_centre c
USING m
WHERE m.d = c.district
  AND 2 * 6371 * asin(sqrt(
        power(sin(radians(c.latitude - m.la) / 2), 2) +
        cos(radians(m.la)) * cos(radians(c.latitude)) * power(sin(radians(c.longitude - m.lo) / 2), 2))) > 120;
