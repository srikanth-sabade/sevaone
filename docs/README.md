# SevaOne documentation

Documents in the SevaOne set share one template (`templates/SevaOne_Document_Template.docx`) and are issued as review baselines until accountable owners approve them.

## Current document set

| Folder | Document | Purpose |
|---|---|---|
| `requirements/` | Software Requirements Specification (v1.1) | Testable product requirements, traceability, acceptance |
| `architecture/` | High Level Design (v1.1) | System responsibilities, trust boundaries, integration, deployment placement |
| `architecture/` | Low Level Design (v1.1) | Implementation design: workers, request pipeline, error contract, rate limits |
| `architecture/` | API Endpoint Reference (v1.0) | Every BFF route with access scheme and rate limit, generated from the controllers |
| `data/` | Database Schema Reference (v1.1) | Schema inventory through Flyway V74, foreign keys, indexes, migration timeline |
| `deployment/` | Kubernetes Deployment Architecture (v1.1) | Production platform design, reference sizing, validation plan, risks |
| `suggested/` | Codebase Recommendations (v1.1) | Engineering findings F01 to F16, kept out of the normative documents |
| `suggested/` | Documentation Review Notes | Decisions and evidence needed to approve the set |

## Supporting material

| Folder | Contents |
|---|---|
| `integration/` | Department integration guide, revenue and Sakala integration notes, WSO2 overview and reference, OTP sample, retired AI backend endpoint list |
| `testing/` | API functional test report and the V1 verification report |
| `reports/` | Project progress report |
| `reference-data/` | Service catalogues, department master lists and Sakala service counts (spreadsheets) |
| `deployment/` | Kubernetes deployment plan presentation (earlier plan, kept for slides) |
| `secrets/` | Credentials and keys. Git-ignored; never share. |
| `DEPARTMENT_GATEWAY_HARDENING_GAP.md` | Older gateway hardening note, referenced by `docker-deploy`; reconcile with recommendation F02 |

## Conventions

- Application code, migrations and configuration define implemented behaviour; these documents describe intent and boundaries.
- Seva-Sindhu-One belongs to another team and is not documented here.
- The API Endpoint Reference is generated from `apps/Seva-One/backend/SevaOne.Services/Controllers`. Regenerate it when a controller changes.
- The earlier SRS, LLD, HLD, schema and Kubernetes plan documents were merged into the set above and retired on 21 September 2026.
