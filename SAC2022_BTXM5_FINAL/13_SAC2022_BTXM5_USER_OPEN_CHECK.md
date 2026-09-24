# SAC 2022 BTXM 5.0 m — USER OPEN/CHECK

## Mục đích
Đây là file kiểm tra trực tiếp trước khi chốt PKT. Không thêm thông số đầu vào mới.

## 1. Inputs đã khóa
| Name | Type | Default |
|---|---|---:|
| W_New | Double | 2.50 |
| Thick_BTXM | Double | 0.20 |
| W_Old | Double | 1.50 |
| Thick_Old | Double | 0.18 |
| Slope_ThietKe | Slope | -2.0% |
| Thick_CPDD | Double | 0.12 |
| Thick_HC | Double | 0.10 |
| W_Le | Double | 1.00 |
| Slope_Le | Slope | -4.0% |
| Slope_Cut | Slope | 1:1 |
| Slope_Fill | Slope | 1:1.5 |

## 2. Targets
- Design_Profile — Elevation
- EG — Surface
- Old_Road — Surface

## 3. Derived condition
Thick_BuVenh_Min is DERIVED, not an Input Parameter.

Business definition:
Zmax_existing - Zmin_existing

Required implementation evidence:
- Old_Road surface
- Auxiliary Surface Link mechanism
- interval 0 -> W_Old
- MaxInterceptY / MinInterceptY or an equally source-proven SAC 2022 mechanism

Do NOT replace this with L6.

## 4. User must inspect in SAC 2022
1. Open the SAC project/PKT.
2. Settings and Parameters: confirm exactly the 11 inputs above.
3. Target Parameters: confirm exactly the 3 targets above.
4. Flowchart: confirm every Sequence and Decision.
5. Select every Point/Link/Shape and inspect Properties.
6. Preview: verify geometry and codes.
7. Verify S5_FILL is actually closed by real SAC geometry.
8. Verify S6_CUT is actually closed by real SAC geometry.
9. Verify no fabricated fixed soil thickness exists.
10. Save and reopen; verify the objects remain unchanged.

## 5. Important
This repository file is NOT itself a fabricated PKT binary. Autodesk documents that SAC projects are composed in the SAC UI, previewed, and then saved as PKT files; PKT import is the subsequent Civil 3D step.

## 6. Items requiring direct SAC 2022 visual confirmation
- Exact S5_FILL link/shape closure.
- Exact S6_CUT link/shape closure.
- Exact Decision routing.
- Exact expression binding for the derived bù vênh condition.

No unsupported topology is silently promoted to final.
