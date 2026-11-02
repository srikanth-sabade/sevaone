# Revision 2 handoff: overview and lifecycles

Updated 18 October 2026.

## Current deliverable

`SevaOne_ServicePlus_Replacement_Phase1_Functional.pptx` now contains 19 slides covering Parts A and B of Revision Request 2. The owner's subsequent instruction to use professional language overrides the previous simplified-English requirement. Explanations use complete sentences and functional terminology.

The earlier versions remain unchanged:
- `SevaOne_ServicePlus_Replacement_Phase1_Functional_37.pptx` — 37 slides.
- `SevaOne_ServicePlus_Replacement_Phase1_Functional_Detailed_94.pptx` — 94 slides.

The current 19-slide deck is the staged review deliverable, not the completed 20-module rebuild. Part C has deliberately not been rebuilt: REVIEW.md requires review of Parts A and B first. Phases 2 and 3 have not started.

## Slide map

Part A, slides 1–9:
1. Functional overview connecting users, access channels, configured services and government partners.
2. Six participant roles and their operational responsibilities.
3. Service configuration, acceptance and publication.
4. Application submission through official delivery.
5. Separate internal, collection-only and external-handoff paths.
6. Payments, documents, notifications and external checks linked to applications.
7. Reports, remedies and protected action history linked to application records.
8. State ownership with delegated department and service-unit access.
9. Karnataka migration programme and cutover.

Part B, slides 10–19:
Citizen, application, service, officer account/posting, payment, certificate, complaint/appeal, notification, state/department onboarding and Karnataka migration lifecycles.

## Changes and evidence boundaries

- Every slide uses native editable diagram objects and connectors. unDraw illustrations are positioned beside the activities they describe. Existing approved assets and their source records remain in `illustrations/`.
- Blue identifies NIC-stated capabilities. Green identifies proposed functional stages and arrangements. Amber identifies uncertainties, conditional returns or recovery paths.
- Lifecycle diagrams explicitly propose stage order and transition rules. The supplied NIC material does not prove an exact internal state machine. Source capability statements are kept separate from those proposed state models.
- The application diagram shows correction and resubmission returning to review, alternative approval/rejection paths, certificate issuance and closure. Action codes 9/10/11 remain “not confirmed”.
- Payment confirmation, application approval and financial matching remain separate. Unclear provider results stay pending. Refunds require finance authority.
- Certificate issuance depends on authorised signing where required. A visible stamp does not prove signature validity.
- Notification failure does not reverse a case decision. Retries and further appeals have explicit return routes.
- State-first ownership remains the replacement proposal and owner requirement, not an assertion about NIC's undisclosed tenant hierarchy.
- Migration preserves original references, signed documents, pending work and financial records. Fallback preserves new decisions and payments. NIC export formats and active-case mappings remain unavailable in the supplied sources.
- The five functional domains remain represented. The dedicated 20-module section follows the requested review gate rather than being appended prematurely.

Evidence basis remains `sources/ServicePlus_PPT.pptx` and `sources/ServicePlus_Detailed_Analysis.docx`. No new audio-derived facts, credentials, personal data, technology selections or hosting assumptions were introduced.

## Review and outstanding decisions

Claude should review Parts A and B before the three-slide-per-module Part C rebuild. REVIEW.md was read and not edited. No other chat was messaged.

Department decisions remain open for identity checks, role authority, jurisdiction, fees, deadlines, signing, appeals, operational ownership and migration acceptance. These are proposals for review, not settled government policies. No application code changed.

## Validation

All 19 final slides were rendered and individually inspected at full size. Text fit and connector routing were repaired before final validation. Package integrity and first-party import passed. Layout validation reported zero findings. The main file matches the validated copy. All three deck versions have zero speaker-note parts or relationships.

Build and validation: `.build/review2/`; final receipt: `.build/review2/output-validation.json`.

Main SHA256: `f47bdba4f93f28cf6b82262af2495e6186050e6685cae9d85c3697fe9016bf69`.
Preserved 37-slide SHA256: `1b2c40e5b605563eef871b0b9d061604bf13a981cb37e07e41106286558b87b3`.
Preserved 94-slide SHA256: `42beba5e4d6f7146e0ceb2fa40826413b49a9295e0bad79512ab0aaeee2d61b4`.
