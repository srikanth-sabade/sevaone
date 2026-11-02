# SevaOne — Architecture Decisions Report

**Prepared for:** Department review
**Scope:** Identity/login integration (WSO2), department-facing API design, the API gateway, and the data-access approach — the four topics reviewed and decided on in this development cycle.

---

## 1. Summary

This report explains four architecture decisions for the SevaOne platform and the reasoning behind each:

| # | Topic | Decision |
|---|---|---|
| 1 | Identity provider (WSO2) | Used for citizen login only. Not used for department-facing calls. |
| 2 | Department integration | Departments call SevaOne's backend directly, using a short-lived, single-use ticket — never WSO2. |
| 3 | API Gateway | A lightweight routing layer is in place today, standing in for a full API Management product; upgrading to a full product (e.g. WSO2 API Manager) is a later, optional step, not a current blocker. |
| 4 | Database access | Application code uses parameterized SQL queries, not stored procedures. Already verified safe against SQL injection; stored procedures were evaluated and found to add cost without adding security. |

Each is expanded below with the reasoning, so this can be reviewed and either approved as-is or flagged for further discussion.

---

## 2. Why WSO2 Is Used — and Only For Login

**What WSO2 does in this system:** confirms a citizen's identity when they log in. Nothing more.

**Why an identity provider is used at all, rather than building login in-house:**
- Login is a security-critical, easy-to-get-wrong piece of software — password handling, OTP, account lockout policy, the standard OAuth2/OIDC login flow. WSO2 Identity Server is a mature, widely audited product built specifically for this.
- It is **self-hosted** by the department — citizen credentials never leave government-controlled infrastructure to a third-party cloud identity service. This matters for a citizen-facing government platform.
- It is open-source and already an established part of Karnataka's e-governance technology stack, not a new dependency introduced for this project.

**Why WSO2's role stops at login, and does not extend to department-facing calls:**
Everything that happens after a citizen logs in — which department, which scheme, whether an application ticket has already been used — is SevaOne's own business logic, not an identity question. WSO2 has no built-in concept of "department," "scheme," or "one-time-use ticket." Extending WSO2 to handle these would mean either:
- Teaching a generic identity product SevaOne-specific business rules (the wrong tool for the job), or
- Duplicating those rules a second time inside WSO2, creating two places the same logic has to be kept in sync — a maintenance and correctness risk.

**Conclusion:** WSO2 is the right tool for proving identity once, at login, and is deliberately not used for anything beyond that.

---

## 3. Why Departments Talk to SevaOne, Not WSO2, Directly

A question was raised: since departments don't call WSO2 directly today, should they? The answer is no, and here is the full reasoning considered:

1. **WSO2 has no concept of "department" or "scheme."** There is nothing meaningful for WSO2 to check on a department's behalf — those rules live only in SevaOne.
2. **Departments are not registered with WSO2.** Only SevaOne is a trusted WSO2 client today. Direct department-to-WSO2 integration would mean onboarding every department into WSO2 individually — repeated setup and credential management for each new department, versus the current single client-secret-per-department relationship with SevaOne only.
3. **WSO2's own login token is not meant to be shared with a third party.** It is scoped for SevaOne's use only. Handing it to a department would expose more trust than intended — a leaked or misused token could potentially be replayed against anything that trusts it for SevaOne's account, not just this one exchange. SevaOne instead mints its own narrowly-scoped, single-purpose ticket for each department handoff.
4. **Only SevaOne can enforce "this ticket may only be used once."** That rule is enforced against SevaOne's own database at the moment a department redeems a ticket. WSO2 has no record of these tickets and could not prevent a replayed/reused ticket.
5. **It keeps every department decoupled from the identity provider.** If SevaOne ever changes how citizens log in, or changes identity providers, no department integration needs to change — departments only ever know about SevaOne's API, never about WSO2 directly.

**Conclusion:** routing department calls through SevaOne, rather than through WSO2, is deliberate and keeps departments insulated from identity-provider details they should never need to know about.

---

## 4. The API Gateway

A lightweight routing layer (built on Microsoft's YARP reverse-proxy technology) is already in place. It routes incoming requests to the correct backend service by URL path and tags each request with a trace ID for debugging.

This is explicitly a **stand-in** for a full API Management product (such as WSO2 API Manager) — it does the routing job needed today, without the additional setup and operational overhead of a full product.

**What a full API Management product would add, if and when needed:**
- A developer portal where external departments can discover and self-service subscribe to SevaOne's APIs
- Formal rate-limiting/throttling per department
- API versioning and lifecycle management (publish, deprecate, retire)
- Usage analytics and dashboards
- Tighter policy integration with WSO2 Identity Server, since both are WSO2 products

**Recommendation:** the lightweight gateway is sufficient for the current stage. Upgrading to a full API Management product should be driven by an actual requirement — e.g. departments needing self-service API onboarding, or a formal rate-limiting/governance mandate — not adopted preemptively.

---

## 5. Database Access: Parameterized Queries vs. Stored Procedures

**Current approach:** the application writes SQL queries directly in code, with all values passed as parameters (never inserted as text into the query itself).

**Security review finding:** this approach was independently audited. Every query in the codebase was checked, and confirmed that no value from a user or external system is ever concatenated into SQL text — all values go through parameter binding, which is the standard, effective defense against SQL injection. **No injection risk was found.**

**Stored procedures were evaluated as an alternative** (moving the SQL into the database itself, called by name from the application). Findings:

| Consideration | Parameterized queries (current) | Stored procedures |
|---|---|---|
| SQL injection risk | None found; parameter binding already prevents it | Same — no additional protection |
| Where code lives | One place: alongside the application code, same review process | Split across two systems (app + database) |
| Deploying a change | One deployment | Two coordinated deployments (database + app), harder to sequence and roll back |
| Debugging | Standard debugger, same as any other code | Cannot be stepped through in the application debugger |
| Reducing damage if credentials leak | Not addressed by this change | Genuinely helps — a database login restricted to executing named procedures only, with no other permissions, limits what a leaked credential can do |

**Conclusion:** stored procedures do not add SQL-injection protection beyond what is already in place, and add real operational cost (two-part deployments, harder debugging, split ownership). The one genuine security benefit they offer — limiting what a leaked database credential can do — can be achieved more simply by restricting the application's database login to only the specific tables and operations it actually needs, without rewriting the data-access layer.

**A pilot conversion was completed** on the single highest-risk operation (the one-time-use application-ticket redemption) to validate the stored-procedure approach end-to-end, on a separate development branch, without affecting the main codebase. It was verified working correctly. The remaining ~59 queries have not been converted, pending a decision on whether the operational cost is justified for the rest of the system.

**Recommendation:** keep the current parameterized-query approach for the full system. If there is a specific compliance or governance requirement mandating stored procedures, that requirement should be confirmed explicitly, since it changes the recommendation.

---

## 6. Open Items for Department Sign-off

1. Confirm no objection to WSO2's scope being limited to login only (Section 2).
2. Confirm no objection to departments integrating only with SevaOne, not WSO2 directly (Section 3).
3. Confirm whether a full API Management product (WSO2 API Manager) is required now, or can remain a future upgrade (Section 4).
4. Confirm whether there is a specific requirement mandating stored procedures for database access, or whether the current parameterized-query approach is acceptable (Section 5).
