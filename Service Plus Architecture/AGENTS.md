# Brief for Codex: Service Plus replacement architecture deck

You are building a PowerPoint (.pptx) that proposes a new architecture and solution for a
government service-delivery platform that replaces NIC's ServicePlus. Claude reviews your work.
The owner (Srikanth) decides. Read this whole file before starting.

## 1. Situation and decisions (settled, do not reopen)

- The Karnataka government does not want to continue with NIC's ServicePlus. They want their own
  platform. This is a **full replacement**, not a wrapper or a modernisation of NIC's system.
- The platform must be **multi-tenant** (state first, then departments and service units inside it).
- **Karnataka's existing services and applications must be imported** into the new platform.
- **Hosting is the government's decision.** Do not assume a cloud or a vendor. Design so the
  choice stays open, and do not name third-party SaaS dependencies.
- The deck proposes, for **every module ServicePlus has**, a newer architecture and a newer solution.

## 2. Source material (read first)

Folder: `~/Desktop/Documents/Service Plus/`
- `Service_Plus_Detailed_Architecture.docx`: replaced by `ServicePlus_Detailed_Analysis.docx`
  (see section 11). Our own summary of ServicePlus and the replacement requirements.
- `ServicePlus_PPT.pptx`: NIC's 22-slide deck (what ServicePlus claims to do).
- `SP Team Api Doc 1.8 (1).pdf`, `SERVICEPLUS_API_INVENTORY.docx`: API material.
- `WhatsApp Audio 2026-10-15 ... .mp4`: conversation with an NIC architect (not transcribed here).

We know ServicePlus **only from the outside**: its deck, a recording, an API doc and what our app
observes. We have never seen its code, schema or deployment.

## 3. Honesty rules (most important)

Label every factual claim about ServicePlus as one of:
- **Known**: stated in the source material (cite the slide or section).
- **Assumed**: our inference. Say so.
- **Proposed**: our design for the new platform.

Never present an assumption as a fact. Never invent endpoint names, schemas, database products or
deployment details for ServicePlus. If the source does not say, write "not disclosed".
Facts that are explicitly uncertain in the sources (for example workflow action codes 9/10/11, the
payment gateway name heard in the audio) must stay marked as unconfirmed.

## 4. The modules

Use these 20. Codex's earlier 18, plus two added by the owner's decisions (19, 20).

Identity and organisation
1. User management and authentication
2. Department and government organisation management
3. Roles and access control
13. Assisted operator and centre management

Service definition
4. Service catalogue and service management
5. Dynamic form builder
6. Business rules and fee configuration
17. Configuration publication and version management

Runtime
7. Workflow designer and execution engine (keep designer and engine as separate responsibilities)
8. Application submission and citizen workspace
9. Official task and case management

Outputs and money
10. Document and certificate management
11. Payments and financial reconciliation
12. Notifications and communication

Platform
14. External integrations and API management
15. Grievances and appeals
16. Reports and dashboards
18. Audit and platform operations
19. Tenant and platform management (new)
20. Migration and import (new)

Three **service kinds** run through everything and must be shown natively in the new platform:
(a) processed inside the platform, (b) collection-only, (c) redirected to another department's system.

## 5. Order of work: three phases, one at a time

Do **not** produce all three at once. After each phase, stop and wait for the owner's approval.

1. **Phase 1: Functional architecture.** What the system does, no technology. Actors, the 20
   modules grouped into the five domains above, the main journeys (apply, process, correct,
   pay, issue, track), the three service kinds, the multi-tenancy view and the migration view.
2. **Phase 2: Solution architecture.** Components, boundaries, data ownership, integrations, key
   choices and trade-offs.
3. **Phase 3: Technical architecture.** Stack, data model, APIs, events, deployment, security.

Start with Phase 1 only.

## 6. Slide pattern for each module

For every module, in this order:
1. **What ServicePlus does today** (Known, with source reference).
2. **Its limits or open questions** (Known or Assumed, labelled).
3. **Proposed architecture** for the new platform.
4. **Proposed solution** (concrete approach and the main design choice, with the alternative rejected).
   In Phase 1 this is a **functional** choice only (for example, one workflow engine for all three
   service kinds versus a separate path per kind). Do not discuss technology, products or
   implementation trade-offs until Phase 2 (components, boundaries) and Phase 3 (stack).

Phase 1 may combine 3 and 4 into one "proposed" slide per module. Keep one idea per slide.

## 7. Visual rules

- Use **unDraw illustrations** wherever possible to show flows.
  - unDraw is free to use commercially without attribution. Download SVGs from undraw.co and save
    them in `illustrations/` in this folder. If you cannot download them, do **not** substitute
    other art. Leave a clearly labelled placeholder and list the illustrations you need.
  - unDraw images are scene illustrations, not diagrams. Draw each flow with real shapes
    (steps and arrows) and place an illustration beside or above each step. Do not rely on an
    illustration alone to carry meaning.
  - Recolour illustrations to the deck's accent colour.
- Flow-heavy content (Phase 1) benefits most. Phase 3 (technical) should use clean boxes and
  lines with few or no illustrations.
- Use real text, native shapes and native charts so the deck stays editable. No full-slide images.
- Keep text legible: no tiny fonts to fit content. Split a slide instead.
- A consistent style across all slides.

## 8. Working agreement between you, Claude and the owner

- **You build the deck. Claude reviews it.** Claude does not edit your files. Claude writes
  comments to `REVIEW.md`. You do not edit `REVIEW.md`.
- Keep a short `HANDOFF.md`: what you produced, what is unfinished, what needs a decision. Update
  it at the end of every session.
- Keep `DECISIONS.md`: each owner decision with date. Do not contradict a logged decision.
- If a question needs the owner, write it in `HANDOFF.md` and stop. Do not guess.
- Do not touch anything outside this folder.
- Do not include secrets, credentials or real personal data in the deck.

## 9. Output location and naming

- `SevaOne_ServicePlus_Replacement_Phase1_Functional.pptx` in this folder.
- Later phases use the same prefix with `Phase2_Solution` and `Phase3_Technical`.

## 10. What done looks like (Phase 1)

- All 20 modules covered, each with the four-part content above.
- Every ServicePlus claim labelled Known / Assumed / Proposed with a source reference.
- The three service kinds, multi-tenancy and migration each have their own journey slide.
- unDraw illustrations are used on the flow slides, or placeholders are listed.
- `HANDOFF.md` lists open questions for the owner.


## 11. Update 18 October 2026 (read `REVIEW.md`)

- The Phase 1 deck must be **30 to 40 slides** (hard limit 40) in **plain English** for readers
  who are not technical. `REVIEW.md` has the rules, word swaps and the slide plan.
- Source files are copied into `sources/`. The main summary is now
  `sources/ServicePlus_Detailed_Analysis.docx`. It replaces the older "Detailed Architecture" file.
- This section and `REVIEW.md` replace the four-slides-per-module rule in section 6 for the main
  deck. Keep the 94-slide version as a separate appendix file.


## 12. Update 18 October 2026 (Revision Request 2 in `REVIEW.md`)

- The deck structure changes: **Part A** the whole of ServicePlus as overview diagrams, **Part B**
  lifecycles (citizen, application, others), **Part C** the 20 modules (layout pending owner
  decision, do not rebuild yet).
- The 30 to 40 slide limit in section 11 is withdrawn. Diagram first, whole plain sentences,
  no fragments, no floating tiny pictures. Details are in `REVIEW.md`.
