# DEVIN MASTER TASK — SAC 2022 BTXM5 COMPLETE BUILD

## Mission
Complete the BTXM5 Subassembly Composer 2022 forensic-to-build task end-to-end. Work only against the installed Autodesk Subassembly Composer 2022 environment and evidence in this repository. Do not use later SAC versions to fill gaps.

## Repository
Repo: duonghai258-beep/ESCONTODTBK
Working branch: forensic/sac2022-btxm5-final-20260924_144728

## Mandatory operating rules
1. INSPECT → PLAN → IMPLEMENT → BUILD → TEST → REVIEW → VALIDATE.
2. Read the entire relevant SAC2022 corpus before changing implementation.
3. Existing files 08 and 12 are candidate/reconciliation artifacts, NOT authoritative truth. Resolve their contradictions from installed SAC 2022 evidence.
4. Never invent an API, property, object relationship, condition operator, PKT schema, soil thickness, geometry, or serialization format.
5. Do not use another SAC release as evidence for SAC 2022.
6. Preserve existing evidence. Do not overwrite forensic artifacts. New findings go into new uniquely named files.
7. Do not fabricate PASS. Every PASS must have reproducible evidence.
8. If a capability cannot be proven, record the exact boundary and continue with every provable part.
9. The final deliverable must be a real SAC 2022-valid artifact if the installed environment permits it—not merely a text description or launcher.
10. Keep the repository clean: no temp, backup, rollback, generated junk, or unrelated files.

## Required corpus
Inspect all existing SAC2022 files in SAC2022_BTXM5_FINAL, especially:
- _SAC2022_FULL_INSPECTION.txt
- 01_OBJECT_LEDGER.csv
- 02_PARAMETERS.csv
- 03_TARGETS.csv
- 04_SAC2022_API_TARGETED.txt
- 05_SAC2022_BTXM5_FINAL_BUILD_SPEC.md
- 08_SAC2022_FLOWCHART_SEQUENCE_DECISION_OBJECT_MODEL_LOCK_20260924.md
- 10_SAC2022_OLDROAD_AUX_SURFACE_LINK_THICK_BUVENH_IMPLEMENTATION_LOCK_20260924.md
- 11_SAC2022_BTXM5_FINAL_DERIVED_CONDITION_LOCK_20260924.md
- 12_SAC2022_BTXM5_S5_S6_FINAL_TOPOLOGY_RECONCILIATION_20260924.md
- 15_SAC2022_API_SCHEMA_PROBE.txt
- 15_SAC2022_API_SCHEMA_PROBE_README.md
- 16_SAC2022_WORKFLOW_GRAPH_SCHEMA_PROBE.ps1 and its output, if present
- all other files under SAC2022_BTXM5_FINAL that materially affect SAC2022.

## Phase A — Installed API/object-model proof
Run/extend probe 16 as needed against the installed SAC 2022 assemblies.

Prove with actual signatures/types:
- Workflow/container model
- Sequence
- Decision/If/Conditional behavior
- Activity nodes and their connections
- CreatePoint/CreateLink/CreateShape
- parameter and target objects
- condition representation/evaluation
- serialization/save/load
- PKT creation/open/reopen path

Do not stop at reflection names. Establish constructor/property/field/method relationships sufficient to build a valid graph.

## Phase B — Authoritative BTXM5 model
Use these exact inputs:
1 W_New = 2.50
2 Thick_BTXM = 0.20
3 W_Old = 1.50
4 Thick_Old = 0.18
5 Slope_ThietKe = -2.0%
6 Thick_CPDD = 0.12
7 Thick_HC = 0.10
8 W_Le = 1.00
9 Slope_Le = -4.0%
10 Slope_Cut = 1:1
11 Slope_Fill = 1:1.5

Targets:
- Design_Profile = Elevation
- EG = Surface
- Old_Road = Surface

Derived value:
Thick_BuVenh_Min = Z_max_existing - Z_min_existing
This is derived, not an invented input or fixed threshold.

## Phase C — Geometry
Preserve/validate the following intended geometry unless installed SAC2022 evidence requires an exact representation change:

P1 Origin
P2 from P1, DX=0, DY=Design_Profile.Elevation-P1.Elevation, target Design_Profile, codes Top,Crown
P3 from P2, DX=W_New, slope=Slope_ThietKe, codes Top,ETW
L1 P2→P3 codes Top,Pave
P4 from P2, DY=-Thick_BTXM, code Subbase
P5 from P3, DY=-Thick_BTXM, codes Subbase,ETW_Sub
P6 from P4, DX=W_Old, slope=Slope_ThietKe, code Subbase_Trong
L2 P4→P6 codes Datum,Nilon_Lop1,Nilon_Lop2
L3 P6→P5 same codes
L4 P2→P4
L5 P3→P5
S1_BTXM P2 P3 P5 P4 code BTXM

P7 from P1, Delta X on Surface, target Old_Road, code Old_Crown
P8 from P7, Delta X on Surface, DX=W_Old, target Old_Road, code Old_ETW
P9 from P7, DY=-Thick_Old, code Old_Datum
P10 from P8, DY=-Thick_Old, code Old_Datum
L6 P7→P8 code Old_Pave
L7 P9→P10 code Old_Datum
L8 P7→P9
L9 P8→P10
S2_OLDROAD P7 P8 P10 P9 code ExistingRoad

L10 P8→P6 code BuVenh
L11 P7→P4 code BuVenh
S3_BUVENH P7 P8 P6 P4 code BuVenh

P11 from P5, DY=-Thick_CPDD, code Top_DatCP
P12 from P6, DY=-Thick_CPDD, code Top_DatCP
L12 P12→P11 code Top_DatCP
L13 P5→P11 code CPDD
L14 P6→P12 code CPDD
S4_CPDD P5 P11 P12 P6 code CPDD

P13 from P3, DX=W_Le, slope=Slope_Le, codes Top,EPS
P14 from P13, Delta X on Surface, target EG, codes EG,Daylight
D1_EG_VALID: EG.IsValid, target EG
D2_DAYLIGHT: P13.Elevation > P14.Elevation
D3_BOX: Z_BottomStructure > Z_EGminusHC, target EG; bind expression to actual SAC objects/variables
P15 from P13, Slope to Surface, slope Slope_Fill, code Daylight_Fill
L15 P13→P15 code Daylight_Fill
P16 from P13, Slope to Surface, slope Slope_Cut, code Daylight_Cut
L16 P13→P16 code Daylight_Cut
S5_FILL P13 P15: fill closure required in SAC; never use fixed soil thickness
S6_CUT P13 P16: cut closure required in SAC; never invent minimum soil thickness

## Phase D — Critical engineering constraints
1. Existing pavement must never be cut.
2. Bottom of new BTXM must be at or above existing-road surface at every relevant offset over the 1.50m existing half-width.
3. Endpoint equality alone is not proof for arbitrary continuous target surfaces. Use a verified continuous mechanism or explicit sampling if SAC2022 supports it.
4. Cut/fill comparison is against EG after the 0.10m organic strip.
5. EG above required bottom => cut.
6. EG below required bottom => variable compacted-soil fill.
7. Fill thickness comes from actual elevations, never a fixed 0.30m.
8. Do not fabricate a minimum soil thickness.

## Phase E — Resolve S5/S6
This is a mandatory investigation, not an assumption.
Determine exactly how SAC2022 represents the branching/closure logic and how Fill/Cut shapes/links are validly created.
Prove:
- condition node semantics
- true/false routing
- geometry creation order
- shape closure requirements
- target/surface interaction
- serialization/reopen behavior

## Phase F — Build
Create the actual SAC2022 project/PKT/required source artifacts using only the proven mechanism.
If direct PKT construction is possible, implement it.
If SAC2022 requires an intermediate project/XML/XAML/object graph, implement the smallest proven path.
If an element can only be created through SAC UI, document the exact boundary and create every automatable part that is proven.

## Phase G — Validation
Perform all possible validation in the installed SAC2022 environment:
- open generated artifact in SAC2022
- verify parameters
- verify targets
- verify flowchart
- verify Sequence/Decision topology
- verify points/links/shapes
- verify codes
- verify preview/simulation
- verify cut/fill behavior
- verify bù vênh non-destructive behavior
- save
- close
- reopen
- revalidate

Create machine-readable evidence plus human-readable final report.

## Required final artifacts
Use new filenames; do not overwrite prior forensic evidence. At minimum produce:
- 17_SAC2022_OBJECT_MODEL_FINAL.md
- 18_SAC2022_BT_XM5_FINAL_TOPOLOGY.csv
- 19_SAC2022_BT_XM5_FINAL_BUILD.ps1 or the actual builder source required by the proven mechanism
- 20_SAC2022_BT_XM5_VALIDATION_REPORT.md
- 21_SAC2022_BT_XM5_EVIDENCE_MANIFEST.csv
- the actual generated SAC2022 project/PKT artifact if creation is proven
- any minimal required test harness/source
Names may be adjusted only if the existing repo naming convention requires it; do not overwrite.

## Final verdict
The final report must separately state:
A. API/object-model proof status
B. topology proof status
C. artifact-generation status
D. SAC2022 open/reopen validation status
E. geometry/engineering validation status
F. exact unresolved boundaries, if any

Never claim complete if a required item is unsupported. Distinguish:
- PROVEN
- NOT PROVEN
- BLOCKED BY INSTALLED SAC2022
with exact evidence.

## Execution instruction to Devin
Do not merely write a plan. Execute the task.
Read, inspect, implement, run, test, review, validate, create artifacts, and commit the completed work.
If a test fails, diagnose and fix it, then rerun.
Continue until all provable work is complete and the final report explicitly accounts for every remaining gap.
