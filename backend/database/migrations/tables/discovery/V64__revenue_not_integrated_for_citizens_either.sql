-- Follow-up to V63: Revenue was left as the one department still marked is_integrated = TRUE,
-- since it's the only one that ever had a real connector (connector_type = 'revenue', the
-- Nadakacheri integration). But that connector's only real consumer -- the Nadakacheri mock
-- backend and its handoff page -- was deleted from this codebase earlier in the same body of
-- work that added ApplyRouteClassifier, so there is currently nothing on the other end that
-- would ever redeem a citizen-minted JWS token for Revenue either. Per explicit product
-- direction: for now, no department has a real citizen-facing integration, Revenue included --
-- flip it to FALSE so citizen apply just redirects directly, same as every other department.
--
-- This does not affect the real, live operator-assisted integrations Revenue already has (4
-- sakala_token services) -- those are gated by scheme.integration_method in
-- ApplyRouteClassifier, entirely independent of this flag. connector_type stays 'revenue' as a
-- historical/descriptive marker; it is not read anywhere in application code.
UPDATE discovery.department_master
SET is_integrated = FALSE
WHERE department_id = 'revenue';
