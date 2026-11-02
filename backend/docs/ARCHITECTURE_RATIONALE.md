# SevaOne compared with the existing Seva Sindhu portal

**Audience:** Department and programme leadership  
**Purpose:** A side-by-side view of the current portal model and the SevaOne model — why the older approach does not fit a statewide department integration, and why SevaOne’s architecture does.

This note does not describe source code or internal implementation. It compares **how the systems work** from a citizen, department, and operations point of view.

---

## 1. In brief

The existing Seva Sindhu portal is built for a **citizen using a browser**: open a page, sign in, fill forms, move to the next page. That job it already does.

What departments need next is different: a **department’s own server** must receive an authenticated citizen, complete the department’s application, and send the citizen back — with one common method for every department, not a custom arrangement each time.

SevaOne is built for that second job. It is a separate service so the live citizen site can keep running while departments adopt a single, documented integration.

---

## 2. What each system is for

**Existing Seva Sindhu**

- A public website for citizens.
- Designed around pages, logins, and clicks in a browser.
- Department links, if any, grew over time as one-off arrangements.
- Changing it in place means changing a site that citizens already use every day.

**SevaOne**

- A citizen portal **plus** a dedicated way for department **systems** to talk to government identity and application tracking.
- One integration for all departments: same steps, same credentials, same errors.
- Built and released on its own, so a problem in the new integration does not take down the current public site.

The older portal is not “wrong” for citizens. It is the **wrong shape** for a uniform, statewide, system-to-system department handoff.

---

## 3. Comparison

### 3.1 Who is talking to whom

**Today**

- The important conversation is between the **citizen’s browser** and the **portal**.
- A department is usually another website the citizen is sent to, or a private arrangement. There is no single, official “department computer calling government” contract.

**SevaOne**

- The citizen still uses a browser on SevaOne.
- When they apply, SevaOne sends them to the department portal with a short-lived pass.
- The **department’s server** then calls SevaOne to confirm that pass and, later, to report that the application is done.
- The citizen’s browser never calls the department API. Only the department’s backend does.

**Why this is better.** A department system is not a person with a browser. It needs a proper, repeatable API — not a cookie meant for a website session.

### 3.2 Identity (who the citizen is)

**Today**

- Login is tied to the portal’s own website session (the browser keeps a session cookie).
- That model assumes the next click comes from the same browser on the same site.
- It does not give every department a government-issued, time-limited proof they can check on their own server.

**SevaOne**

- The citizen proves identity on SevaOne (mobile OTP if they already have an account; DigiLocker / Aadhaar OTP if they are new).
- SevaOne then creates a proper citizen session using a **self-hosted** identity platform (WSO2), so credentials stay on government infrastructure.
- The department never sees that login session. It only sees a **one-time ticket** for this application, then a **separate ticket** to send the citizen home.

**Why the older model is not enough.** Sharing a website session with an outside department would mean trusting another organisation with the same login the citizen uses on the main portal. A stolen or reused browser session is the wrong tool for a department computer.

**Why SevaOne is better.** Login stays with SevaOne. The department gets only what it needs for this visit, once, and then that pass expires.

### 3.3 How a new department is added

**Today**

- Each department tends to mean a **new special case**: different links, different data, different support.
- There is no single document a new department can follow and be done.
- Adding a department often means more work on the live portal.

**SevaOne**

- Every department follows the **same two steps**: confirm the citizen (`User Details`), then tell SevaOne the case is filed (`Apply Complete`).
- Onboarding is credentials and configuration, not a new custom project for each department.
- Errors come back as stable codes so the department’s system can handle them automatically.

**Why the older model does not scale.** Fifty departments as fifty one-off builds cannot be operated or secured as one programme.

**Why SevaOne is better.** Department N+1 is an onboarding exercise, not a rewrite.

### 3.4 Security boundary

**Today**

- The public website is the main door. Access control is built around people in browsers.
- There is no dedicated “department applications only” front door with per-department keys and rate limits.

**SevaOne**

- Department traffic goes through a **published API gateway**.
- Each department has an API key and its own client credentials.
- Direct access to SevaOne’s internal server is not the supported path.
- Handoff and return tickets can be used **once**. A copied link cannot be reused.

**Why the older model is weaker for this use.** A citizen website was not designed as a high-assurance API for other government systems. Mixing both jobs in one live site enlarges what a single outage or mistake can break.

**Why SevaOne is better.** Citizen browsing and department machines are separated. A leak of a handoff ticket is limited in time and use. A leak of a citizen login is not handed to departments at all.

### 3.5 Change and downtime

**Today**

- A large new feature on the live portal is a change to the same system citizens already depend on.
- A fault in that feature can affect login, existing services, and existing department links together.

**SevaOne**

- The new integration can be tested, scaled, and patched on its own.
- The current Seva Sindhu site can keep serving citizens during that work.
- Departments can be moved onto the new contract one at a time.

**Why this is the safer path.** Statewide integration is high-impact. It should not be bolted onto the only production citizen website in a single release.

---

## 4. Why not simply “upgrade the old portal”?

Upgrading screens or adding one more department page does not create:

- a uniform contract every department can implement from a guide;
- a machine API that does not depend on a browser session;
- isolation so a department-integration incident is not a whole-portal incident;
- a way to change how citizens log in later without asking every department to rebuild.

Those are the requirements of a **platform**. The older portal is a **website**. Platforms and websites can share a brand; they should not share the same failure domain.

SevaOne can still look and feel like Seva Sindhu to the citizen. That is a product choice. The architecture choice is to keep the **department integration** off the live website’s critical path.

---

## 5. Summary

| Concern | Existing Seva Sindhu | SevaOne |
|---|---|---|
| Primary user | Citizen in a browser | Citizen in a browser, plus department **systems** |
| Department join-up | Typically bespoke, hard to repeat | One published contract for all |
| Proof sent to department | Not a standard, one-time, server-checked pass | One-time tickets; department server confirms with SevaOne |
| Login shared with departments | Website session is the wrong tool | Departments never receive the citizen login |
| Adding a department | Often more live-site work | Credentials and the same two APIs |
| Risk to the live public site | High if this work is done in place | Isolated; current site can keep running |

**SevaOne is the better architecture for statewide department integration** because it is built for system-to-system trust, one contract, and independent operation.

**The older portal is not the better architecture for that job** because it is built for browser sessions and pages. Extending it in place would mix two different problems and put live citizen service at unnecessary risk.

---

## 6. What leadership is asked to accept

1. Keep SevaOne as a **separate** service; do not fold this integration into the live Seva Sindhu site as an in-place rewrite.  
2. Keep **one** department contract — not a custom protocol per department.  
3. Keep **citizen login** on SevaOne (and DigiLocker where required); departments receive only the application handoff, not the citizen’s portal session.
