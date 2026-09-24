# SAC 2022 BTXM5 — Dependency Graph & S5/S6 Gap Audit

Date: 2026-09-24
Scope: Autodesk Civil 3D / Subassembly Composer 2022 only
Evidence basis: package files on this branch + targeted SAC 2022 installed-API inventory already captured in `04_SAC2022_API_TARGETED.txt`.
No other Civil 3D/SAC version or unrelated PKT was used to fill gaps.

## 1. Result

The supplied package contains a complete explicit point/link topology for S1_BTXM, S2_OLDROAD, S3_BUVENH and S4_CPDD.

It does **not** contain enough executable object evidence to lock S5_FILL or S6_CUT as closed SAC shapes.

The missing information is not merely a shape label. The package is missing the geometry objects and/or variable bindings required to construct the closing boundary.

Therefore:
- S5_FILL: **NOT LOCKABLE FROM CURRENT EVIDENCE**
- S6_CUT: **NOT LOCKABLE FROM CURRENT EVIDENCE**
- D3_BOX: **NOT LOCKABLE FROM CURRENT EVIDENCE**
- Thick_HC: declared parameter, but no executable geometry expression in the supplied ledger consumes it.

No P17/P18/P19/P20 or equivalent objects are invented by this audit.

## 2. SAC 2022 object-model evidence

The targeted installed SAC 2022 inventory records:

- `PktFileAccess.OpenPkt(String) -> PktStructure`
- `PktFileAccess.CreatePktFile(PktStructure,String) -> Void`
- `CreatePoint`: Get/SetPropertyValue, SubValidateInDesigntime, GeneratesPoint, DependsOnPoint
- `CreateLink`: SubValidateInDesigntime, DependsOnPoint
- `CreateShape`: SubValidateInDesigntime
- `Geometry.GetPoint/GetLink/GetShape`
- `Shape.CompareWith/ContainAllLinks/IsInsideShape`
- `WorkflowHost.Execute(...)`

These APIs establish that point/link/shape objects and their dependency/validation relationships are part of the SAC 2022 workflow model. They do not, by themselves, provide undocumented missing S5/S6 geometry.

Autodesk's Civil 3D 2022 documentation likewise describes the flowchart as the place where subassembly behavior and geometry are composed and the geometry is previewed/verified. citeturn0search0turn0search2

## 3. Parameter -> expression -> geometry dependency audit

### Parameters explicitly present

| Parameter | Used by explicit geometry ledger | Result |
|---|---|---|
| W_New | P3 | USED |
| Thick_BTXM | P4, P5 | USED |
| W_Old | P6, P8 | USED |
| Thick_Old | P9, P10 | USED |
| Slope_ThietKe | P3, P6 | USED |
| Thick_CPDD | P11, P12 | USED |
| Thick_HC | none | **ORPHANED** |
| W_Le | P13 | USED |
| Slope_Le | P13 | USED |
| Slope_Cut | P16 | USED |
| Slope_Fill | P15 | USED |

The package therefore has 10 parameters consumed by explicit geometry and 1 parameter (`Thick_HC`) declared without a consuming point/link/shape expression.

### Targets explicitly present

- Design_Profile -> P2
- Old_Road -> P7, P8
- EG -> P14, P15, P16 and D1/D3 metadata

The target list itself is complete for the three targets declared by the package.

## 4. Point dependency graph

Explicit point parents:

- P1 <- Origin
- P2 <- P1
- P3 <- P2
- P4 <- P2
- P5 <- P3
- P6 <- P4
- P7 <- P1
- P8 <- P7
- P9 <- P7
- P10 <- P8
- P11 <- P5
- P12 <- P6
- P13 <- P3
- P14 <- P13 + EG target
- P15 <- P13 + EG target
- P16 <- P13 + EG target

There is no explicit P17+ row in the supplied object ledger.

Consequently, any S5/S6 construction requiring a new point cannot be claimed to exist in the supplied package.

## 5. Link dependency graph

Explicit links:

- L1: P2 -> P3
- L2: P4 -> P6
- L3: P6 -> P5
- L4: P2 -> P4
- L5: P3 -> P5
- L6: P7 -> P8
- L7: P9 -> P10
- L8: P7 -> P9
- L9: P8 -> P10
- L10: P8 -> P6
- L11: P7 -> P4
- L12: P12 -> P11
- L13: P5 -> P11
- L14: P6 -> P12
- L15: P13 -> P15
- L16: P13 -> P16

There is no explicit link whose endpoints create a second boundary for S5 or S6.

## 6. Shape closure audit

### S1_BTXM

Declared polygon:
P2 -> P3 -> P5 -> P4 -> P2

All required boundary links exist:
L1, L5, L4, and L? closure P4->P2 is the reverse of L4.

Status: **EXPLICIT CLOSED TOPOLOGY**

### S2_OLDROAD

Declared polygon:
P7 -> P8 -> P10 -> P9 -> P7

Boundary links exist:
L6, L9, L7, L8.

Status: **EXPLICIT CLOSED TOPOLOGY**

### S3_BUVENH

Declared polygon:
P7 -> P8 -> P6 -> P4 -> P7

Boundary links exist:
L6, L10, L2, L11.

Status: **EXPLICIT CLOSED TOPOLOGY**

Important limitation: this proves graph closure only. It does not prove the stated non-destructive bù vênh condition for every offset over the entire target surface.

### S4_CPDD

Declared polygon:
P5 -> P11 -> P12 -> P6 -> P5

Boundary links exist:
L13, L12, L14, L3.

Status: **EXPLICIT CLOSED TOPOLOGY**

### S5_FILL

Declared source objects:
- P13
- P15
- L15
- shape code DatCP
- note: variable fill to CPDD bottom

Only one boundary segment is explicitly available:
P13 -> P15 (L15).

A closed polygon requires another boundary returning from P15 to P13. The package supplies no second explicit link and no additional fill-bottom point.

Status: **NOT LOCKABLE**

### S6_CUT

Declared source objects:
- P13
- P16
- L16
- shape code DaoKhuon
- note: excavation/organic stripping

Only one boundary segment is explicitly available:
P13 -> P16 (L16).

The package supplies no explicit excavation-bottom point, organic-strip boundary point, or closing link.

Status: **NOT LOCKABLE**

## 7. D3_BOX audit

D3 expression:

`Z_BottomStructure > Z_EGminusHC`

Search of the supplied package ledger/specification yields:
- no point named `Z_BottomStructure`
- no point named `Z_EGminusHC`
- no variable-definition object
- no Set Variable object/row
- no expression row defining either symbol
- no point/link consuming `Thick_HC`

Therefore D3 cannot currently be bound to executable SAC geometry without inventing missing objects.

## 8. What is actually missing

For S5, the package must provide evidence for all of the following before it can be locked:

1. The elevation/point representing the CPDD bottom at the fill boundary.
2. The exact point used to close the fill polygon back to P15.
3. The exact CreateLink object(s) closing the polygon.
4. The variable/expression that calculates fill thickness from actual elevations.
5. The Decision/Sequence connection that activates the fill shape.

For S6:

1. The excavation/organic-strip bottom reference.
2. The exact point(s) defining the cut shape's lower boundary.
3. The exact CreateLink object(s) closing the polygon.
4. The expression involving the 0.10 m organic strip, if that is intended by the design.
5. The Decision/Sequence connection that activates the cut shape.

For D3:

1. Definition of `Z_BottomStructure`.
2. Definition of `Z_EGminusHC`.
3. Binding of those definitions to actual SAC point/target/variable objects.
4. The branch connection to the fill/cut sequence.

## 9. What must NOT be invented

The current evidence does not justify inventing:

- P17/P18/P19/P20
- a fixed fill thickness
- a fixed excavation thickness
- a MAX/MIN formula
- a new target
- a hidden soil layer
- a new CPDD extension
- a new organic-strip geometry
- a topology copied from another Civil 3D/SAC version
- a topology copied from another PKT

## 10. Audit conclusion

The correct forensic state is:

| Object | Evidence state | Can be locked? |
|---|---|---|
| P1-P16 | Explicit ledger evidence | YES |
| L1-L16 | Explicit ledger evidence | YES |
| S1 | Closed explicit topology | YES |
| S2 | Closed explicit topology | YES |
| S3 | Closed explicit graph topology | YES, with continuous bù vênh limitation |
| S4 | Closed explicit topology | YES |
| S5 | Only one explicit edge; closure missing | **NO** |
| S6 | Only one explicit edge; closure missing | **NO** |
| D1 | Explicit expression EG.IsValid | YES |
| D2 | Explicit expression P13.Elevation > P14.Elevation | YES |
| D3 | Undefined symbols in package | **NO** |
| Thick_HC | Declared but unconsumed | **NO executable binding** |

This is an evidence result, not a design choice. The next valid step is to obtain the actual SAC 2022 project/PKT or a SAC 2022 flowchart/object export containing the missing variable, decision, point and link objects. Without that evidence, S5/S6 must remain open rather than fabricated.

## 11. Autodesk 2022 documentation boundary

Autodesk's Civil 3D 2022 documentation states that the Subassembly Composer uses a flowchart to define behavior, geometry elements are placed in the flowchart, and the resulting geometry is previewed and verified. citeturn0search0turn0search2

That documentation supports the object-model/flowchart interpretation above; it does not supply the missing BTXM5-specific S5/S6 objects. No later-version behavior was used to fill the missing package evidence.
