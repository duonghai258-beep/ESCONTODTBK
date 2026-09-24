# SAC 2022 BTXM5 — Flowchart / Sequence / Decision / Object Model LOCK
Date: 2026-09-24
Scope: Autodesk Civil 3D / Subassembly Composer 2022 only.

## 1. New source evidence changes the earlier gap result

The newly supplied detailed build text explicitly defines Sequence_1 through Sequence_5, Decision_1 and Decision_2, P17-P21, L17-L26, branch-specific S5/S6 definitions, Decision_1 condition EG.HasTarget, Decision_2 condition P13.Y>P14.Y, and Thick_HC consumption through P16/P17.

Therefore the earlier conclusion that P17+ do not exist in the design specification is superseded by this newly supplied source. The earlier ledger is incomplete relative to this source.

## 2. SAC 2022 object model

The captured SAC 2022 API evidence establishes PktFileAccess/PktStructure as package/workflow objects; CreatePoint as point activity with property/dependency/validation methods; CreateLink as link activity with dependency/validation; CreateShape as shape activity with validation; Geometry.GetPoint/GetLink/GetShape as geometry lookup; and WorkflowHost.Execute as workflow execution.

Autodesk Civil 3D 2022 documents the concrete Decision(condition) -> Sequence pattern for target-dependent behavior. It also defines points as vertices, links as segments, and shapes as closed cross-sectional polygons. LinkMulti documentation explicitly states that shapes are not automatically closed and a closing link must be included.

## 3. Locked flowchart

Sequence_1 -> Sequence_2 -> Sequence_3 -> Decision_1 [EG.HasTarget]
Decision_1 TRUE -> Sequence_4 -> Decision_2 [P13.Y > P14.Y]
Decision_2 TRUE -> fill daylight geometry; FALSE -> cut daylight geometry.
Decision_1 FALSE -> Sequence_5.

## 4. Sequence_1 — new BTXM

P1 Origin; P2 Design_Profile DX=0; P3 from P2 DX=W_New slope=Slope_ThietKe; L1 P2->P3.
P4 from P2 DY=-Thick_BTXM; P5 from P3 DY=-Thick_BTXM; P6 from P4 DX=W_Old slope=Slope_ThietKe.
L2 P4->P6 codes Datum/Nilon_Lop1/Nilon_Lop2; L3 P6->P5 same codes; L4 P2->P4; L5 P3->P5.
S1 = L1/L2/L3/L4/L5, code BTXM.

## 5. Sequence_2 — existing road / bù vênh

P7 Old_Road DX=0; P8 from P7 DX=W_Old slope=Slope_ThietKe; L6 P7->P8.
P9 from P7 DY=-Thick_Old; P10 from P8 DY=-Thick_Old; L7 P9->P10; L8 P7->P9; L9 P8->P10.
S2 = L6/L7/L8/L9, code DuongCu.
L10 P8->P6; L11 P7->P4; S3 = L6/L10/L11/L2 according to supplied source, code BuVenh.

## 6. Sequence_3 — CPDD

P11 from P5 DY=-Thick_CPDD; P12 from P6 DY=-Thick_CPDD.
L12 P12->P11 code Top_DatCP; L13 P5->P11; L14 P6->P12.
S4 = L3/L12/L13/L14, code CPDD.

## 7. Decision_1

Condition exactly as supplied: EG.HasTarget.
TRUE -> Sequence_4. FALSE -> Sequence_5.
Autodesk 2022 supports target-validity decisions controlling whether a sequence is applied. The exact expression member EG.HasTarget must still be validated in the actual SAC 2022 expression editor before claiming runtime equivalence to IsValid.

## 8. Sequence_4 — low-ground / fill branch

P13 from P3 DX=W_Le slope=Slope_Le; L15 P3->P13 codes Top/Le.
P14 from P13, Slope to Surface, target EG, vertical slope Double.PositiveInfinity, code Daylight.
P15 from P12, Slope to Surface, target EG, vertical slope Double.PositiveInfinity, code Daylight.
P16 from P15 DX=0 DY=-Thick_HC code Subgrade_Trong; P17 from P14 DX=0 DY=-Thick_HC code Subgrade_EPS.
L16 P16->P17 codes Datum/DayKhuon; L17 P12->P16; L18 P13->P17.
Source-defined S5_FILL = L12 + L15 + L18 + L16 + L17, code DatCP.

## 9. Sequence_4 daylight decision

Decision_2 condition: P13.Y > P14.Y.
TRUE: Slope to Surface from P13 to EG with Slope_Fill; codes Daylight/Daylight_Fill.
FALSE: Slope to Surface from P13 to EG with Slope_Cut; codes Daylight/Daylight_Cut.

## 10. Sequence_5 — high-ground / cut branch

P18 from P11 DY=-0.3 code Subgrade_Mep; P19 from P12 DY=-0.3 code Subgrade_Trong.
L19 P19->P18 codes Datum/DayKhuon; L20 P11->P18; L21 P12->P19.
S5_CUT_BRANCH = L12/L19/L20/L21, code DatCP.
P20 from P3 DX=1.0 slope=Slope_Le codes Top/EPS; L23 P3->P20 codes Top/Le.
P21 from P20, Slope to Surface, target EG, vertical slope, code Daylight.
L24 = excavation wall P18->P20; L25 = P20->P21 codes Daylight/Daylight_Cut; L26 = P3->P21 surface-following natural-ground cut boundary.
S6_CUT = L26/L25/L24/L20/L13, code DaoKhuon.

## 11. Critical topology reconciliation — S5 fill branch

The supplied S5 boundary list is not graph-connected with the supplied endpoints:
L12: P12->P11
L15: P3->P13
L18: P13->P17
L16: P16->P17
L17: P12->P16
The graph has two disconnected components: P12-P11 and P12-P16-P17-P13-P3.
Therefore S5_FILL is NOT yet a mathematically closed single polygon from the supplied endpoint definitions.
Possible source corrections are: L15 actually starts at P11; or an additional link connects P11 to P13; or another endpoint/object definition is missing. No correction is invented.

## 12. S6 low-ground branch reconciliation

The supplied S6 description says it is bounded by natural ground segment P15-P14 and L16, with L17/L18 as side walls, but no explicit link object P15->P14 is listed.
Therefore the intended S6 low-ground boundary is identified semantically but is not executable-locked until the P15-P14 boundary object is identified or confirmed.

## 13. S6 cut branch reconciliation

The supplied S6 cut list is L26/L25/L24/L20/L13.
Using the stated endpoints: L26 P3->P21; L25 P20->P21; L24 P18->P20; L20 P11->P18; L13 P5->P11.
This chain does not return to P3; it starts at P3 and ends at P5.
Therefore S6 cut is also NOT closed as written unless an omitted link P5->P3 exists or an endpoint definition is different. No omitted link is invented.

## 14. Object-level verdict

| Object | Evidence | Status |
|---|---|---|
| Sequence_1 | explicit source | LOCKED |
| Sequence_2 | explicit source | LOCKED |
| Sequence_3 | explicit source | LOCKED |
| Decision_1 | explicit condition | CONDITION LOCKED; runtime expression verification pending |
| Sequence_4 | explicit source | OBJECTS LOCKED |
| Decision_2 | explicit condition | LOCKED |
| Sequence_5 | explicit source | OBJECTS LOCKED |
| P17-P21 | explicit source | LOCKED AS SOURCE DEFINITIONS |
| L17-L26 | explicit source | LOCKED AS SOURCE DEFINITIONS |
| S5 fill | explicit list | TOPOLOGY INCONSISTENT |
| S6 low-ground | semantic boundary; missing explicit P15-P14 link | TOPOLOGY INCOMPLETE |
| S5 cut branch | explicit list | CLOSED |
| S6 cut branch | explicit list | TOPOLOGY INCONSISTENT |
| Thick_HC | P16/P17 consume it | CONNECTED |

## 15. Final forensic conclusion

The missing information is no longer which P17-P21 objects exist. The supplied source gives those objects.
The remaining forensic problem is the exact endpoint definitions of several boundary links. Those definitions must be reconciled against the actual SAC 2022 flowchart/PKT object data before S5/S6 can be declared executable and closed.
