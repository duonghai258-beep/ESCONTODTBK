# SAC 2022 — Thick_BuVenh_Min Derived-Condition Forensic Lock
Date: 2026-09-24
Scope: Autodesk Civil 3D / Subassembly Composer 2022
Repository: duonghai258-beep/ESCONTODTBK
Branch: forensic/sac2022-btxm5-final-20260924_144728

## 1. Decision

Thick_BuVenh_Min is NOT an Input Parameter.

It is a derived/calculated value used by a condition:

    Thick_BuVenh_Min = Z_max_existing - Z_min_existing

The existing-road surface is the Old_Road target. For the currently modelled one-half-side geometry, the calculation scope is the existing-road surface interval represented by the Old_Road surface link.

Therefore the parameter classification is:

- Input Parameters: 11
- Target Parameters: 3
- Derived value: Thick_BuVenh_Min
- User-entered value: NO

## 2. SAC 2022 mechanism established by evidence

The targeted SAC 2022 API evidence already contains:

- Autodesk.SubassemblyComposer.WorkflowEngine.Link
  - MaxInterceptY(Double slope)
  - MinInterceptY(Double slope)
- Autodesk.SubassemblyComposer.WorkflowEngine.SALink
  - MaxInterceptY(Double slope)
  - MinInterceptY(Double slope)
- Autodesk.SubassemblyComposer.WorkflowEngine.PreviewSurfaceTarget
  - sampleSection(Double offset1, Double offset2)
- Autodesk.SubassemblyComposer.WorkflowEngine.SurfaceTarget
  - getPointOnOffset(Double offset)
  - sampleSection(Double offset1, Double offset2)

The API inventory is in:
SAC2022_BTXM5_FINAL/04_SAC2022_API_TARGETED.txt

Autodesk documentation also establishes that an Auxiliary Surface Link can be drawn on a target surface between specified offsets and that its geometry can be used by subsequent geometry. Autodesk's road-rehabilitation material documents MaxY/MinY and MaxInterceptY/MinInterceptY as link API functions for existing-surface analysis.

## 3. Derived calculation

Use an Auxiliary Surface Link over the Old_Road target, covering the exact existing-road interval to be analysed.

For a horizontal reference (slope = 0), the derived extrema are:

    Z_max_existing = L_OLD_SURF.MaxInterceptY(0)
    Z_min_existing = L_OLD_SURF.MinInterceptY(0)

Therefore:

    Thick_BuVenh_Min =
        L_OLD_SURF.MaxInterceptY(0)
        - L_OLD_SURF.MinInterceptY(0)

Equivalent MaxY/MinY semantics are documented for links, but the current package's targeted API inventory explicitly exposes MaxInterceptY/MinInterceptY. Do not invent a different MAX/MIN function.

## 4. Important scope constraint

This calculation is only valid for the surface interval actually represented by L_OLD_SURF.

For the current one-half-side BTXM model, that interval is the Old_Road half-width:

    0 -> W_Old

If the business requirement means the entire roadway width across both sides of the centerline, the PKT must represent/sample both sides before claiming a full-road Zmax/Zmin result. Do not silently treat a half-side result as a full-road result.

## 5. Condition integration

Thick_BuVenh_Min is a calculated variable/value, not a user input.

The condition consuming it must be expressed separately from the calculation. The exact Boolean condition has NOT been invented in this artifact because the business rule threshold/operator has not yet been supplied.

Examples such as:
    Thick_BuVenh_Min > 0
or
    Thick_BuVenh_Min > Thick_BTXM
must NOT be inserted unless the engineering rule explicitly requires them.

## 6. Relationship to existing bù vênh geometry

Current evidence has:

- P7/P8 = existing Old_Road target points
- P4/P6 = lower/new-structure-side points
- L10 = P8 -> P6, code BuVenh
- L11 = P7 -> P4, code BuVenh
- S3_BUVENH = P7 P8 P6 P4, code BuVenh

This artifact does not alter those objects.

The derived minimum bù vênh value is an analysis/condition value and must not be confused with the geometric endpoint elevation difference represented by L10/L11.

## 7. Forensic status

LOCKED:
- Thick_BuVenh_Min is derived, not an Input Parameter.
- Formula concept: Zmax_existing - Zmin_existing.
- SAC 2022 has link MaxInterceptY/MinInterceptY mechanisms in the inspected API.
- A surface-link-based implementation is technically supported by documented SAC mechanisms.

PENDING RUNTIME/PKT VALIDATION:
- Exact Auxiliary Surface Link object placement and offsets in the final PKT.
- Exact Expression Editor syntax accepted by the installed SAC 2022 build.
- Exact Boolean condition that consumes Thick_BuVenh_Min.
- Whether the required business scope is one half-side or the full existing roadway.

NO FABRICATION:
- No new user Input Parameter is added.
- No unsupported MAX/MIN expression is invented.
- No condition threshold is invented.
- Existing S3/L10/L11 topology is not changed by this audit.
