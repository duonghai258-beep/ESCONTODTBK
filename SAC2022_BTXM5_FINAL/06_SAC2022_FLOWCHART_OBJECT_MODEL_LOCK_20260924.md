# SAC 2022 BTXM5 — FLOWCHART / OBJECT MODEL LOCK
Date: 2026-09-24
Scope: Autodesk Subassembly Composer 2022 / Civil 3D 2022 only
Package: SAC2022_BTXM5_FINAL
Branch: forensic/sac2022-btxm5-final-20260924_144728

## 1. LOCKED SAC 2022 OBJECT MODEL

### 1.1 PKT container
Installed SAC 2022 package evidence identifies:
- Autodesk.SubassemblyComposer.FileAccess.PktFileAccess
  - CreatePktFile(PktStructure, String)
  - OpenPkt(String) -> PktStructure
  - LoadPktStructure(String) -> PktStructure
- Autodesk.SubassemblyComposer.FileAccess.PktStructure

Therefore the authoritative package container is PktStructure loaded/saved through PktFileAccess. This report does not infer an undocumented XML schema or invent a PKT writer.

### 1.2 Geometry objects
The installed SAC 2022 API evidence identifies:
- ActivityLibrary.CreatePoint
  - GeneratesPoint(String)
  - DependsOnPoint(String)
  - GetPropertyValue / SetPropertyValue / GetProperties
  - SubValidateInDesigntime
- ActivityLibrary.CreateLink
  - DependsOnPoint(String)
  - SubValidateInDesigntime
- ActivityLibrary.CreateShape
  - SubValidateInDesigntime
- WorkflowEngine.Geometry
  - GetPoint(String)
  - GetLink(String)
  - GetShape(String)
  - GenerateDummyShapes(...)
- WorkflowEngine.Shape
  - CompareWith(Shape)
  - ContainAllLinks(Shape)
  - IsInsideShape(Shape)

This locks the structural interpretation used below: Point activities generate points; Link activities depend on points; Shape is a separate geometry object whose validity is tied to its link topology. A Shape is not a two-point line.

### 1.3 Runtime execution
The installed SAC 2022 API identifies WorkflowHost.Execute(...) and ExecutedStatus. Therefore the flowchart is executable workflow, not merely a drawing description.

### 1.4 Surface target
The installed SAC 2022 API identifies SurfaceTarget:
- getDaylightPoint(Point, Double, Boolean)
- getPointOnOffset(Double)
- hasDaylightPoint(Point, Double, Boolean)
- sampleSection(Double, Double)

This confirms that target-surface geometry and daylight are native SAC 2022 operations. It does NOT prove that an arbitrary continuous minimum/maximum across a surface is implicit in a Shape or Decision.

## 2. LOCKED FLOWCHART SEMANTICS

### 2.1 Sequence
Sequence is the flow organization mechanism. Geometry must be placed in executable order so that dependencies exist before consumers.

For this package the dependency order is therefore:
P1/P2/P3 -> BTXM geometry -> existing-road geometry -> bù vênh -> CPDD -> shoulder/EG -> branch geometry.

A point/link cannot be treated as available merely because it appears in a documentation table; its generating activity must occur before dependent activities.

### 2.2 Decision
A Decision has:
- one Condition expression;
- a True branch;
- a False branch.

The package has:
- D1_EG_VALID: EG.IsValid
- D2_DAYLIGHT: P13.Elevation > P14.Elevation
- D3_BOX: Z_BottomStructure > Z_EGminusHC

D1 and D2 are expressible against currently evidenced objects.
D3 is NOT locked because the package contains no evidenced definitions for Z_BottomStructure or Z_EGminusHC.

Autodesk SAC 2022 documentation states that a Decision draws the True branch when the condition is met and the False branch otherwise. The installed package also contains Set Variable Value, but its SAC 2022 documentation requires the target variable to have been defined previously. Therefore an undefined D3 variable cannot be silently treated as a valid variable.

## 3. LOCKED CURRENT TOPOLOGY

### S1_BTXM
Closed loop:
P2 -> P3 -> P5 -> P4 -> P2
Links: L1, L5, L4, plus the P4-P6/P6-P5 material geometry remains outside this shape.
Status: topologically complete.

### S2_OLDROAD
Closed loop:
P7 -> P8 -> P10 -> P9 -> P7
Links: L6, L9, L7, L8.
Status: topologically complete.

### S3_BUVENH
Proposed closed loop:
P7 -> P8 -> P6 -> P4 -> P7
Links: L6, L10, L2, L11.
Status: graph-closed, but this does NOT prove the non-destructive bù vênh condition over every offset. The package itself correctly states that endpoint checking is insufficient for arbitrary surfaces.

### S4_CPDD
Closed loop:
P5 -> P11 -> P12 -> P6 -> P5
Links: L13, L12, L14, L3.
Status: topologically complete.

## 4. EXACT FINDING FOR S5_FILL

Current package definition:
- P13 = shoulder/expansion top edge.
- P15 = EG daylight point from P13 using Slope_Fill.
- L15 = P13 -> P15, Daylight_Fill.
- S5_FILL is currently described only as "P13 P15 / Fill closure required in SAC".

This is NOT a valid closed Shape topology.

The exact missing topology is not just one closing link. A valid S5 fill region requires a closed boundary representing:
1. the upper/structural boundary from the road structure toward the shoulder;
2. the fill/daylight boundary P13 -> P15;
3. the lower/base boundary at the actual variable fill elevation;
4. the connection back to the structural boundary.

At minimum, the current two-node definition is missing the lower boundary geometry and its attachment points. The package contains no P17/P18/P19/P20 or equivalent bottom-boundary definitions. It also contains no evidenced expression defining the required bottom elevation at P13/P15.

Therefore the exact point IDs, link IDs, and their geometry cannot be reconstructed uniquely from this package without inventing evidence.

S5 verdict: BLOCKED — topology under-specified in package evidence.

## 5. EXACT FINDING FOR S6_CUT

Current package definition:
- P13 = shoulder/expansion top edge.
- P16 = EG daylight point from P13 using Slope_Cut.
- L16 = P13 -> P16, Daylight_Cut.
- S6_CUT is currently described only as "P13 P16 / Cut closure required in SAC".

This is NOT a valid closed Shape topology.

The exact missing topology is the complementary lower/structural excavation boundary. A valid cut region requires:
1. the structural-bottom boundary;
2. the cut/daylight boundary P13 -> P16;
3. the boundary representing the post-organic-strip condition;
4. the closing connections between those boundaries.

Again, the package contains no evidenced bottom-cut points or links and no evidenced geometry defining the required bottom boundary. No unique P17/P18/etc. construction is justified.

S6 verdict: BLOCKED — topology under-specified in package evidence.

## 6. THICK_HC / D3 CONSEQUENCE

Thick_HC = 0.10 exists as an input parameter.

However:
- no point uses -Thick_HC;
- no defined variable named Z_EGminusHC is present;
- no defined variable named Z_BottomStructure is present;
- no P17/P18/etc. bottom-boundary geometry is present.

Therefore Thick_HC is currently orphaned from executable geometry. D3 cannot be considered a valid locked Decision until those dependencies are explicitly created and evidenced.

## 7. WHAT IS NOW PROVEN

Proven from SAC 2022 + package:
- PktFileAccess/PktStructure are the package object-model entry points.
- CreatePoint/CreateLink/CreateShape are the geometry activity types.
- Point dependency and Link dependency are explicit API concepts.
- Shape has link-containment/topology operations.
- Decisions are True/False workflow branches.
- Set Variable Value requires a previously defined variable.
- SurfaceTarget supports daylight and offset sampling.
- S1-S4 have explicit closed point/link topology.
- S5/S6 do not have complete topology in the supplied package.

Not proven and therefore not filled:
- exact bottom-fill point(s);
- exact cut/excavation bottom point(s);
- exact links closing S5;
- exact links closing S6;
- exact Z_BottomStructure definition;
- exact Z_EGminusHC definition;
- any undocumented MAX/minimum expression;
- any topology copied from another SAC/Civil 3D version.

## 8. LOCK DECISION

S5 and S6 are now locked as structural gaps, not as guessed geometry.

The correct next implementation step is to obtain package evidence that defines the missing structural-bottom and post-organic-strip boundary. Until that evidence exists, adding arbitrary P17/P18/P19/P20 or arbitrary closing links would violate the package-only / SAC 2022-only rule.
