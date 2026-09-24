# SAC 2022 — Old_Road Auxiliary Surface Link + Thick_BuVenh_Min Implementation Lock
Date: 2026-09-24
Branch: forensic/sac2022-btxm5-final-20260924_144728

## 1. Purpose

Implement Thick_BuVenh_Min as a DERIVED value, not an Input Parameter.

Business definition:

    Thick_BuVenh_Min = Z_max_existing - Z_min_existing

The source surface is the existing-road target:

    Old_Road : Surface

The current model is one half-side, with:

    Old_Road interval = X 0 .. W_Old
    W_Old = 1.50

## 2. Exact SAC 2022 geometry mechanism

Use an Auxiliary Surface Link, not L6.

Autodesk SAC documentation defines Auxiliary Surface Link as a link drawn on a target surface between specified offsets. Its properties include Surface Target, Start X, End X, Start/End Offset Target, Depth, and generated start/end points. Autodesk's example specifically states that this mechanism is useful in road rehabilitation to find the maximum Y of an existing road surface.

Therefore the forensic object specification is:

    Object type       = Auxiliary Surface Link
    Link ID           = AL_OLD_ROAD_EXTREMA   (working forensic name)
    Surface Target    = Old_Road
    Start X           = 0
    End X             = W_Old
    Start Offset      = unset
    End Offset        = unset
    Depth             = 0
    Start Point Name  = generated/verified by SAC
    End Point Name    = generated/verified by SAC

Do not rename the actual generated AL number until the installed SAC 2022 UI/PKT serialization confirms the accepted identifier.

## 3. Derived value

For the verified auxiliary surface link:

    Z_max_existing = AL_OLD_ROAD_EXTREMA.MaxInterceptY(0)
    Z_min_existing = AL_OLD_ROAD_EXTREMA.MinInterceptY(0)

Then:

    Thick_BuVenh_Min =
        AL_OLD_ROAD_EXTREMA.MaxInterceptY(0)
        - AL_OLD_ROAD_EXTREMA.MinInterceptY(0)

SAC 2022 API evidence in 04_SAC2022_API_TARGETED.txt exposes MaxInterceptY(Double) and MinInterceptY(Double) on Link and SALink. Autodesk SAC documentation states these functions use the highest/lowest intercept and explicitly identifies their use in rehabilitation subassemblies.

## 4. Why L6 is not used

Current L6 is:

    P7 -> P8
    Type = ordinary Link
    Code = Old_Pave

It is not evidence of an Auxiliary Surface Link.

Do not replace the required AL with L6 merely because both span the existing-road interval.

## 5. Why no MAX/MIN surface expression is invented

The package now has evidence for:

- Auxiliary Surface Link as a SAC geometry element.
- Surface Target binding.
- Start X / End X.
- MaxInterceptY / MinInterceptY on Link/SALink.
- SAC 2022 road-rehabilitation example using an Auxiliary Surface Link to obtain maximum existing-road elevation.

The remaining implementation validation is only the actual installed SAC 2022 object serialization and expression-editor acceptance.

## 6. Condition boundary

Thick_BuVenh_Min is available to a Decision/condition after AL_OLD_ROAD_EXTREMA has executed.

The actual Boolean operator/threshold is deliberately NOT invented here.

The following are NOT locked:

    Thick_BuVenh_Min > 0
    Thick_BuVenh_Min >= Thick_BTXM
    Thick_BuVenh_Min >= any fixed value

A condition may only be locked after the engineering rule specifies what Thick_BuVenh_Min is being tested against.

## 7. Important mathematical scope

This computes the elevation range of the Old_Road surface represented by the one-half-side interval 0..W_Old.

It does NOT, by itself, prove the global maximum/minimum of an entire roadway across both sides.

If the final engineering requirement means both sides of the roadway, the same surface-analysis mechanism must be instantiated over both required intervals and the global extrema combined. That must not be silently assumed.

## 8. Current package status

LOCKED:
- 11 user Input Parameters.
- 3 Targets.
- Thick_BuVenh_Min is derived, not input.
- Source target = Old_Road.
- Analysis mechanism = Auxiliary Surface Link.
- Analysis interval = 0..W_Old for the current one-half-side model.
- Derived formula = MaxInterceptY(0) - MinInterceptY(0).
- L6 is not substituted for the auxiliary surface link.

PENDING:
- Actual SAC 2022 PKT object serialization/identifier.
- Exact expression-editor syntax in the installed SAC 2022 instance.
- Exact Boolean condition consuming Thick_BuVenh_Min.
- If required, extension from one half-side to whole-road extrema.

NO FABRICATION:
- No new input parameter.
- No fixed bù-vênh threshold.
- No unsupported MAX/MIN function.
- No change to S3/L10/L11 topology.
