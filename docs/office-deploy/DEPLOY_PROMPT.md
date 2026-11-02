# Prompt to give Claude on the office server

Copy everything between the lines into the Claude session that has access to the office server.
Attach / copy `catalogue_replace.sql` (same folder) to the server first.

------------------------------------------------------------

You are deploying the latest SevaOne code and catalogue to the OFFICE server. Be careful: other
things already run there and must not be touched.

## Hard rules
- Change ONLY these three app containers: `bff`, `frontend`, `admin-panel`. Do not stop, restart,
  recreate or change wso2is, wso2apim, postgres, redis, kafka, nginx, token bridge or anything else.
- Never run `docker compose down`, `docker system prune`, `docker volume rm`, or delete any volume.
- Do not edit `.env` files, certificates, nginx config or `docs/secrets`. Do not print secrets.
- Take a backup before every change. Show me each plan step and what it will touch before running it.
- If anything is different from what is described here, STOP and ask me.

## 1. Look first (read-only)
- Find how SevaOne is deployed on this server (compose file(s), folder, which containers run).
- Show: running containers, current git commit/branch of the deployed code, free disk space.
- Check that nothing is using the apps right now (tell me and wait if users are active).

## 2. Get the latest code
- Repo: https://github.com/srikanth-sabade/sevaone  (branch `main`, latest commit).
- Fetch it into a SEPARATE folder or update the existing checkout with a plain fast-forward.
  Do not discard local changes on the server. If the server has local edits (compose files, env,
  config templates), keep them and tell me what differs.

## 3. What changed (for your information)
- Backend (.NET): Seva Sindhu token integration. New `EdcsKeyValue.cs`; Key1..Key5 are chosen per
  service by the admin (not used / Aadhaar hash / ask the citizen / fixed value / citizen field).
  Apply asks for a missing email and the admin-named questions in one dialog before the token is made.
  The apply-token endpoint now accepts an optional JSON body `{ "answers": {...} }`.
- Admin panel: Token integrations page has Key1-Key4 (Key5 under Advanced); V3 and "we post to
  department" types are hidden.
- Frontend: Apply dialog (email + named questions).
- No database schema migration was added by this work. If the server's normal deploy runs
  migrations, let it do so as usual.
- New setting on bff: `TokenIntegration__EdcsApi__BaseUrl`
  (default in the compose file: https://sevasindhu.karnataka.gov.in/EP0115_SevaOne_API_UAT/api/TokenIntegration).
  Keep the UAT address unless I tell you otherwise. Add this variable to the server's bff config only if missing.

## 4. Rebuild and restart only the three apps
- Build `bff`, `frontend`, `admin-panel` only, then `up -d --no-deps bff frontend admin-panel`.
- Wait until healthy. Show the container status and the last bff log lines (errors only).

## 5. Catalogue (database) -- do this AFTER the apps work
File: `catalogue_replace.sql` (26 departments, 51 sub-departments, 686 services, 1372 role-visibility
rows, 276 token integrations). It REPLACES the catalogue tables in schema `discovery`.
1. Take a full backup first: `pg_dump -Fc` of the whole database to a dated file. Show me its size.
2. Check for rows that depend on the old catalogue. Show me counts for:
   orchestration.applications, orchestration.operator_wallet_txn, orchestration.department_sessions,
   orchestration.dept_user_ref, portal.sevaone_token, status.payment_status_post,
   discovery.sevaone_service_document, discovery.department_onboarding, discovery.token_integration_attempt.
   If ANY of these has rows, STOP and ask me. Do not delete them.
3. Only if all are empty (or I approve): run the file in ONE transaction:
   `psql -v ON_ERROR_STOP=1 -1 -f catalogue_replace.sql`  (it already contains BEGIN/COMMIT; it rolls back on any error).
4. Verify counts are exactly: department_master 26, sub_department_master 51,
   sevaone_service_master 686, service_role_visibility 1372, token_integration 276.
5. Restart ONLY `bff` (it caches the catalogue for 5 minutes) and check health.

## 6. Verify
- Open the citizen site: the department list shows 26 departments; a service page shows an Apply button.
- Open the admin panel: Token integrations lists the services.
- Report what you checked. Do not test Apply with real citizen data without asking me.

## 7. Rollback (tell me how, do not run unless I ask)
- Apps: rebuild/restart the previous git commit. Database: restore the `pg_dump` taken in step 5.1.
------------------------------------------------------------
