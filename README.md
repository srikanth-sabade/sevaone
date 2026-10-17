# sevaone

The .NET SevaOne stack: web backend, citizen frontend, admin panel, plus the infra that ties
them together.

```
backend/         .NET backend (SevaOne.Services, SevaOne.Business, SevaOne.Infrastructure,
                  SevaOne.Domain, tests)
frontend/        Citizen-facing web portal (React)
admin-panel/     Admin/operator-facing panel (React)
docker-deploy/   Compose files, WSO2 IS/APIM config, nginx, bring-up scripts -- the full local
                  stack (postgres, redis, kafka, WSO2 IS, WSO2 APIM, these three apps)
docs/            Architecture, API reference, deployment and integration docs for this stack
```

This is a separate stack from the mobile app and its own backend (Flutter app + FastAPI
backend + citizen web portal all live in a different repo, `mobile-sevaone`) -- the mobile app
never calls anything here.

## Running locally

`docker-deploy/scripts/setup-stack.sh` brings up the whole stack (infra + WSO2 IS/APIM + these
three apps). See `docker-deploy/NEW_MACHINE_SETUP.md` for first-time setup.

**Before running anything**, copy the real secrets into `docker-deploy/.env` yourself --
it's gitignored and was deliberately not included in this repo (DigiLocker keys, WSO2 client
secrets, SMS gateway credentials, signing keys). Get these from whoever holds them; nothing
here will start without them.
