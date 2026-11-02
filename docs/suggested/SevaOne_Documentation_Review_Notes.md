# SevaOne documentation review notes

**Date:** 20 September 2026  
**Purpose:** Unresolved evidence and decisions needed to promote the new document set from review baseline to approved baseline. These notes are separate from the HLD, LLD, SRS, schema reference and deployment architecture.

| Priority | Decision or evidence | Suggested owner | Reason / reference |
|---|---|---|---|
| High | Approve production workload: active RPS, concurrent connections, login burst, endpoint mix, latency and availability SLO; reconcile with the 19-node sizing before procurement. | Product + platform | `docs/deployment/SevaOne_Kubernetes_Deployment_Architecture.docx` sections 5 and 12 (reference sizing and the unapproved 500,000-connection target) |
| High | Confirm Kubernetes distribution/owner, network and zone model, GitOps workflow, and production manifest repository. The current checkout contains Compose but no production Kubernetes manifests. | EDCS platform | Deployment architecture (section 15) expects manifests, policies, overlays, pipelines, secret references and runbooks in a deployment repository. |
| High | Compare deployed schema with Flyway through V74 and repeatable checksums using a schema-only export; resolve drift. | Database owner | No live SevaOne database was available; the schema document derives from migrations. |
| High | Set RPO/RTO per data class and approve backup scope, isolation, restore cadence and evidence. | Service/data owner | Deployment architecture (section 7) identifies recovery objectives as open decisions. |
| Medium | Approve government tier-to-screen/API/report matrix, including geographic/department scope. | Government product | SRS acceptance depends on a complete scope matrix. |
| Medium | Approve accessibility standard/level and Kannada review/translation ownership. | Product/accessibility | No approved standard or process was identified in the reviewed reference set. |
| Medium | Approve retention, deletion, correction, analytics aggregation and audit schedules for identity, application, clickstream and AI data. | Privacy/legal/data | Existing references identify the decision but do not specify an approved schedule. |
| Medium | Obtain signed contracts for DigiLocker/CeG, OTP, WSO2, Sakala/Seva Sindhu/AJSK and department APIs, including callback identity, idempotency, timeout, errors and key rotation. | Integration owners | Interface specifics remain provider/environment-dependent. |
| Medium | Assign owners for Kubernetes, database, WSO2/APIM, ingress, AI, secrets, telemetry and incident response. | EDCS/platform | Needed for production support and recovery. |

## Notes

- The database reference is based on versioned migrations through V74 plus repository repeatables. It is not a live schema export.
- Docker Compose is documented for developer/local workflows only; Kubernetes is the production target.
- The 19-node plan is a proposal. The small-host Compose estimate is not production sizing. Validate all capacity claims with a representative load test.
- Static codebase/API hardening findings remain in `docs/suggested/SevaOne_Codebase_Recommendations.docx`; keep those findings out of the normative architecture and requirements documents.
- The earlier SRS, LLD, HLD, schema and Kubernetes plan documents were retired on 21 September 2026 after their retained content was merged into the current set. The current files are review baselines until accountable owners approve them.
- The API endpoint reference (`docs/architecture/SevaOne_API_Endpoint_Reference.docx`) is generated from the controllers; regenerate it whenever a controller changes.
- Recommendation F16 (department test-token route outside integration) was added on 21 September 2026.

## Promotion checklist

1. Resolve capacity, Kubernetes ownership, database parity and recovery decisions.
2. Assign document reviewers and record approvals/version.
3. Trace SRS requirements to design, tests and release evidence.
4. Publish production manifests/runbooks in the designated deployment repository.
5. Reissue the schema reference from an approved environment export after Flyway and drift verification.
