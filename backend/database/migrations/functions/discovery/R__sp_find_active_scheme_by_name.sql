-- Postgres function backing SevaOne's Dapper repositories.
-- Naming/style rationale: see backend/docs/POSTGRES_FUNCTIONS_VS_PROCEDURES.md.
-- Flyway "R__" repeatable migration: re-applied automatically whenever this file's content
-- changes (CREATE OR REPLACE is idempotent), unlike versioned V__ migrations which run once.
--
-- Backs the AI apply flow (see ApplyClickTrackingService.ResolveAndTrackAiApplyAsync):
-- sevaone-rag is a fully separate catalog from this one, with its own free-text department
-- labels and its own service numbering that never lines up with this catalog's
-- department_master.department_id / sevaone_service_master.service_id - the ONLY reliable
-- link between the two is the service's real display NAME, which is why this looks up by
-- name (case/whitespace-insensitive exact match) rather than by any code.
--
-- p_department_code is an optional disambiguating hint (sevaone-rag maps its own free-text
-- department label to a best-guess real department code where it recognizes one - see
-- app/agent/department_mapping.py there). When given, a same-named scheme in that department is
-- preferred over an exact-name match elsewhere.
--
-- p_operator_hint is a SECOND, narrower fallback tier, tried only when no exact match exists at
-- all: the real catalog's regional bus-pass variants are named with the operator appended as a
-- suffix ("Issue of Bus Passes to School Children - KSRTC"), while sevaone-rag keeps the
-- operator in its own department field and leaves the bare service name unsuffixed - these can
-- never exact-match. Deliberately NOT solved with fuzzy/similarity matching: calibrated live,
-- genuinely different service variants (e.g. "Caste Certificate (SC/ST)" vs "...(Cat-A)") score
-- HIGHER on trigram similarity (0.67-0.72) than this fix has any business trusting, which risks
-- silently misdirecting a citizen to the wrong variant - a real government service, not a place
-- to guess. This fallback is deterministic instead: requires BOTH the bare name to be an exact
-- PREFIX of the real name (never a same-shape-different-variant match like the caste example,
-- since one is never a strict prefix of the other) AND the caller's own already-known specific
-- operator substring to appear in it - so it can never cross-match a different operator's page
-- for the same base service name. p_operator_hint is only ever sent by
-- department_mapping.map_department_to_operator_hint for the regional bus-pass family
-- specifically; every other caller leaves it null and gets exact-match-only behavior.
CREATE OR REPLACE FUNCTION discovery.sp_find_active_scheme_by_name(
    p_service_name character varying(300), p_department_code character varying(64) DEFAULT NULL,
    p_operator_hint character varying(32) DEFAULT NULL)
RETURNS TABLE (
    "Id" uuid, "ServiceCode" character varying(32), "DepartmentRowId" uuid,
    "DepartmentCode" character varying(64), "Name" character varying(300)
)
LANGUAGE sql STABLE AS $$
    WITH exact_match AS (
        SELECT s.id, s.service_id, s.department_id, d.department_id AS dept_code, s.service_name
        FROM discovery.sevaone_service_master s
        JOIN discovery.department_master d ON d.id = s.department_id
        WHERE lower(trim(s.service_name)) = lower(trim(p_service_name))
          AND s.is_active = TRUE AND d.is_active = TRUE
        ORDER BY (d.department_id = p_department_code) DESC NULLS LAST
        LIMIT 1
    ),
    operator_suffix_match AS (
        -- Strips a trailing "(Student)"-style parenthetical before the prefix check - confirmed
        -- live, sevaone-rag's own KKRTC/NWKRTC school-children records carry this cosmetic
        -- category tag directly appended to the name with no separating space
        -- ("...Children(Student)"), which would otherwise never prefix-match the real catalog's
        -- bare "...Children - NWKRTC" naming at all.
        SELECT s.id, s.service_id, s.department_id, d.department_id AS dept_code, s.service_name
        FROM discovery.sevaone_service_master s
        JOIN discovery.department_master d ON d.id = s.department_id
        WHERE p_operator_hint IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM exact_match)
          AND s.service_name ILIKE regexp_replace(trim(p_service_name), '\s*\(Student\)\s*$', '', 'i') || '%'
          AND s.service_name ILIKE '%' || p_operator_hint || '%'
          AND s.is_active = TRUE AND d.is_active = TRUE
        LIMIT 1
    )
    SELECT id, service_id, department_id, dept_code, service_name FROM exact_match
    UNION ALL
    SELECT id, service_id, department_id, dept_code, service_name FROM operator_suffix_match
    LIMIT 1;
$$;
