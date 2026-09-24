# SAC 2022 BTXM5 — FINAL DERIVED CONDITION LOCK
Date: 2026-09-24
Branch: forensic/sac2022-btxm5-final-20260924_144728

## 1. Input parameter decision

The 11 user input parameters are sufficient for this model.

No additional user input is added for Thick_BuVenh_Min.

Autodesk documents input parameters as values the user can enter/change; Thick_BuVenh_Min is instead calculated from Old_Road and therefore remains derived. citeturn2search0turn0search7

## 2. Auxiliary Surface Link

Create one Auxiliary Surface Link for the current one-half-side model:

    Surface Target = Old_Road
    Start X       = 0
    End X         = W_Old
    Start Offset Target = unset
    End Offset Target   = unset
    Depth         = 0

Autodesk's SAC 2022 documentation explicitly defines these properties and states that the element is intended for analysis of a target surface and road rehabilitation. citeturn0search2

The installed SAC 2022 forensic corpus independently exposes:

    Autodesk.SubassemblyComposer.ActivityLibrary.AuxSurfaceLinkDesigner
    Autodesk.SubassemblyComposer.ActivityLibrary.CreateAuxLink
    Autodesk.SubassemblyComposer.WorkflowEngine.Link.MaxInterceptY(Double)
    Autodesk.SubassemblyComposer.WorkflowEngine.Link.MinInterceptY(Double)
    Autodesk.SubassemblyComposer.WorkflowEngine.SALink.MaxInterceptY(Double)
    Autodesk.SubassemblyComposer.WorkflowEngine.SALink.MinInterceptY(Double)

## 3. Derived value

Define:

    Z_max_existing = AL_OLD_ROAD_EXTREMA.MaxInterceptY(0)
    Z_min_existing = AL_OLD_ROAD_EXTREMA.MinInterceptY(0)

Then:

    Thick_BuVenh_Min = Z_max_existing - Z_min_existing

Autodesk defines MaxInterceptY as the highest intercept and MinInterceptY as the lowest intercept, and specifically documents these functions for rehabilitation subassemblies. citeturn0search0

## 4. Condition

Because Thick_BuVenh_Min is the elevation range between the maximum and minimum existing-road elevations:

    Thick_BuVenh_Min > 0

is the derived condition for the existence of a non-flat bù-vênh requirement.

This is NOT a user-supplied engineering threshold and does not introduce a fabricated minimum thickness.

Branches:

    TRUE  -> bù vênh branch is required
    FALSE -> no elevation-range bù vênh is required

Do not compare Thick_BuVenh_Min against Thick_BTXM, Thick_CPDD, 0.10, 0.20, or another arbitrary value.

## 5. Important distinction

The condition above determines whether an elevation range exists.

It does NOT by itself prove that the complete S3_BUVENH polygon is a faithful representation of an arbitrary Old_Road surface. The existing S3 geometry must still be reconciled with the actual target-surface analysis if the final requirement is to model the full irregular surface rather than only its extrema.

## 6. Non-destructive rule remains mandatory

The new BTXM underside must not cut the existing pavement.

The Auxiliary Surface Link provides the extrema analysis. It must not be replaced by L6, because L6 is an ordinary Link in the current ledger.

## 7. No extra inputs

Final user inputs remain exactly:

1. W_New = 2.50
2. Thick_BTXM = 0.20
3. W_Old = 1.50
4. Thick_Old = 0.18
5. Slope_ThietKe = -2.0%
6. Thick_CPDD = 0.12
7. Thick_HC = 0.10
8. W_Le = 1.00
9. Slope_Le = -4.0%
10. Slope_Cut = 1:1
11. Slope_Fill = 1:1.5

Targets remain exactly:

- Design_Profile | Elevation
- EG | Surface
- Old_Road | Surface

## 8. Remaining build validation

The design inputs are now closed.

Remaining work is implementation validation only:

- instantiate the Auxiliary Surface Link in SAC 2022;
- confirm the generated AL identifier;
- enter the exact expressions in the SAC 2022 Expression Editor;
- connect the derived condition to the bù-vênh branch;
- reconcile S5/S6 closure endpoints against the actual flowchart;
- save and reopen the resulting PKT with PktFileAccess/OpenPkt.

No undocumented PKT XML or binary is fabricated.
