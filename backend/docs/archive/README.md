# Archive

Docs moved here because they're either superseded by a current doc, or describe a flow that's
been deleted from the codebase. Kept for history, not maintained — don't treat anything here as
accurate about the system today.

| File | Why it's archived |
|---|---|
| `API_FLOW.md` | Superseded by `../COMPLETE_FLOW_WITH_PAYLOADS.md` (same purpose, kept current). Still references the `seva.sid` citizen session cookie, which no longer exists — replaced by real WSO2 access/refresh tokens. |
| `E2E_CHECKLIST.md` | References the `seva.opr`/`seva.sid` cookies and a WSO2 password-grant login fallback, both deleted from the code. |
| `FULL_PLATFORM_JOURNEY.md` | Same — describes the pre-WSO2-redesign login/session model, which no longer exists. |
| `ARCHITECTURE_DECISIONS_REPORT.md` | A point-in-time deliverable ("prepared for department review"), not a living reference — never meant to track ongoing changes. |
| `CITIZEN_LOGIN_FLOW.html` | Pre-dates the WSO2 identity redesign; superseded by `../USER_JOURNEY.md`'s current diagrams. |

Current, maintained docs live one level up in `docs/`.
