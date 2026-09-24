# SAC 2022 — BTXM 5.0 m rehabilitation / expansion

## 1. Scope
- Autodesk Civil 3D / Subassembly Composer 2022 only.
- Model one half-side: 2.50 m new pavement; mirror/use both sides.
- Existing road half-width 1.50 m; existing thickness 0.18 m.
- Expansion strip 1.00 m; CPDD 0.12 m; organic strip 0.10 m.
- Cut slope 1:1; fill slope 1:1.5.
- Existing pavement must never be cut.
- No fabricated minimum soil thickness.

## 2. Parameters
- W_New | Double | 2.50 | Half width of new BTXM
- Thick_BTXM | Double | 0.20 | New concrete thickness
- W_Old | Double | 1.50 | Half width of existing road
- Thick_Old | Double | 0.18 | Existing pavement thickness
- Slope_ThietKe | Slope | -2.0% | Design crossfall
- Thick_CPDD | Double | 0.12 | CPDD thickness
- Thick_HC | Double | 0.10 | Organic stripping thickness
- W_Le | Double | 1.00 | Expansion/shoulder strip
- Slope_Le | Slope | -4.0% | Shoulder slope
- Slope_Cut | Slope | 1:1 | Cut slope
- Slope_Fill | Slope | 1:1.5 | Fill slope

## 3. Targets
- Design_Profile | Elevation | Design profile target
- EG | Surface | Existing ground target
- Old_Road | Surface | Existing old-road target

## 4. Point/Link/Shape ledger
- **POINT P1** | From=Origin | Type=Origin | DX= | DY/Slope= | Target= | Codes= | Assembly origin
- **POINT P2** | From=P1 | Type=Delta X and Delta Y | DX=0 | DY/Slope=Design_Profile.Elevation-P1.Elevation | Target=Design_Profile | Codes=Top,Crown | New crown
- **POINT P3** | From=P2 | Type=Slope and Delta X | DX=W_New | DY/Slope=Slope_ThietKe | Target= | Codes=Top,ETW | New edge
- **LINK L1** | From=P2->P3 | Type=Link | DX= | DY/Slope= | Target= | Codes=Top,Pave | BTXM top
- **POINT P4** | From=P2 | Type=Delta X and Delta Y | DX=0 | DY/Slope=-Thick_BTXM | Target= | Codes=Subbase | BTXM underside at crown
- **POINT P5** | From=P3 | Type=Delta X and Delta Y | DX=0 | DY/Slope=-Thick_BTXM | Target= | Codes=Subbase,ETW_Sub | BTXM underside at edge
- **POINT P6** | From=P4 | Type=Slope and Delta X | DX=W_Old | DY/Slope=Slope_ThietKe | Target= | Codes=Subbase_Trong | BTXM underside at old-road edge
- **LINK L2** | From=P4->P6 | Type=Link | DX= | DY/Slope= | Target= | Codes=Datum,Nilon_Lop1,Nilon_Lop2 | Two nylon sheets are represented by codes on the interface
- **LINK L3** | From=P6->P5 | Type=Link | DX= | DY/Slope= | Target= | Codes=Datum,Nilon_Lop1,Nilon_Lop2 | Two nylon sheets
- **LINK L4** | From=P2->P4 | Type=Link | DX= | DY/Slope= | Target= | Codes= | BTXM thickness
- **LINK L5** | From=P3->P5 | Type=Link | DX= | DY/Slope= | Target= | Codes= | BTXM thickness
- **SHAPE S1_BTXM** | From=P2 P3 P5 P4 | Type=Closed Shape | DX= | DY/Slope= | Target= | Codes=BTXM | New concrete only
- **POINT P7** | From=P1 | Type=Delta X on Surface | DX=0 | DY/Slope= | Target=Old_Road | Codes=Old_Crown | Existing road top at center
- **POINT P8** | From=P7 | Type=Delta X on Surface | DX=W_Old | DY/Slope= | Target=Old_Road | Codes=Old_ETW | Existing road top at edge
- **POINT P9** | From=P7 | Type=Delta X and Delta Y | DX=0 | DY/Slope=-Thick_Old | Target= | Codes=Old_Datum | Existing road underside center
- **POINT P10** | From=P8 | Type=Delta X and Delta Y | DX=0 | DY/Slope=-Thick_Old | Target= | Codes=Old_Datum | Existing road underside edge
- **LINK L6** | From=P7->P8 | Type=Link | DX= | DY/Slope= | Target= | Codes=Old_Pave | Existing road surface
- **LINK L7** | From=P9->P10 | Type=Link | DX= | DY/Slope= | Target= | Codes=Old_Datum | Existing road underside
- **LINK L8** | From=P7->P9 | Type=Link | DX= | DY/Slope= | Target= | Codes= | Existing road boundary
- **LINK L9** | From=P8->P10 | Type=Link | DX= | DY/Slope= | Target= | Codes= | Existing road boundary
- **SHAPE S2_OLDROAD** | From=P7 P8 P10 P9 | Type=Closed Shape | DX= | DY/Slope= | Target= | Codes=ExistingRoad | Existing only; never count as new material
- **LINK L10** | From=P8->P6 | Type=Link | DX= | DY/Slope= | Target= | Codes=BuVenh | Bù vênh edge
- **LINK L11** | From=P7->P4 | Type=Link | DX= | DY/Slope= | Target= | Codes=BuVenh | Bù vênh center
- **SHAPE S3_BUVENH** | From=P7 P8 P6 P4 | Type=Closed Shape | DX= | DY/Slope= | Target= | Codes=BuVenh | New material above existing road
- **POINT P11** | From=P5 | Type=Delta X and Delta Y | DX=0 | DY/Slope=-Thick_CPDD | Target= | Codes=Top_DatCP | CPDD bottom at new edge
- **POINT P12** | From=P6 | Type=Delta X and Delta Y | DX=0 | DY/Slope=-Thick_CPDD | Target= | Codes=Top_DatCP | CPDD bottom at old-road edge
- **LINK L12** | From=P12->P11 | Type=Link | DX= | DY/Slope= | Target= | Codes=Top_DatCP | CPDD bottom line
- **LINK L13** | From=P5->P11 | Type=Link | DX= | DY/Slope= | Target= | Codes=CPDD | CPDD thickness
- **LINK L14** | From=P6->P12 | Type=Link | DX= | DY/Slope= | Target= | Codes=CPDD | CPDD thickness
- **SHAPE S4_CPDD** | From=P5 P11 P12 P6 | Type=Closed Shape | DX= | DY/Slope= | Target= | Codes=CPDD | Crushed aggregate base
- **POINT P13** | From=P3 | Type=Slope and Delta X | DX=W_Le | DY/Slope=Slope_Le | Target= | Codes=Top,EPS | Shoulder/expansion edge
- **POINT P14** | From=P13 | Type=Delta X on Surface | DX=0 | DY/Slope= | Target=EG | Codes=EG,Daylight | EG at shoulder edge
- **DECISION D1_EG_VALID** | From= | Type=Condition | DX= | DY/Slope=EG.IsValid | Target=EG | Codes= | Target validity gate
- **DECISION D2_DAYLIGHT** | From= | Type=Condition | DX= | DY/Slope=P13.Elevation > P14.Elevation | Target= | Codes= | Surface daylight branch
- **DECISION D3_BOX** | From= | Type=Condition | DX= | DY/Slope=Z_BottomStructure > Z_EGminusHC | Target=EG | Codes= | Road-box cut/fill condition; expression must be bound to actual SAC points/variables
- **POINT P15** | From=P13 | Type=Slope to Surface | DX= | DY/Slope=Slope_Fill | Target=EG | Codes=Daylight_Fill | Fill daylight
- **LINK L15** | From=P13->P15 | Type=Link | DX= | DY/Slope= | Target= | Codes=Daylight_Fill | Fill slope
- **POINT P16** | From=P13 | Type=Slope to Surface | DX= | DY/Slope=Slope_Cut | Target=EG | Codes=Daylight_Cut | Cut daylight
- **LINK L16** | From=P13->P16 | Type=Link | DX= | DY/Slope= | Target= | Codes=Daylight_Cut | Cut slope
- **SHAPE S5_FILL** | From=P13 P15 | Type=Fill closure required in SAC | DX= | DY/Slope= | Target= | Codes=DatCP | Variable fill to CPDD bottom; do not use fixed thickness
- **SHAPE S6_CUT** | From=P13 P16 | Type=Cut closure required in SAC | DX= | DY/Slope= | Target= | Codes=DaoKhuon | Excavation/organic stripping; do not invent soil thickness

## 5. Non-destructive bù vênh rule
- Required condition: bottom of new BTXM must be at or above existing-road surface at every relevant offset over the 1.50 m half-width.
- Endpoint equality/inequality alone is not a mathematical proof for arbitrary target surfaces.
- Therefore the final SAC implementation must either use a verified continuous/target mechanism or explicitly sample and compare the required offsets. The script does not invent an unsupported MAX expression.

## 6. Cut/fill rule
- Required structural-bottom elevation is compared against EG after the 0.10 m organic strip.
- If EG is above the required bottom: road-box excavation/cut branch.
- If EG is below the required bottom: variable compacted-soil fill branch.
- Fill thickness is calculated from actual elevations; it is not fixed at 0.30 m.

## 7. Important SAC 2022 implementation facts
- Use target validity as `EG.IsValid`.
- Use `Delta X on Surface` for a point at a specified offset on a target surface.
- Use `Slope and Delta X` for a point defined by slope and horizontal offset.
- Use Sequences to organize flow and Decisions to branch.

## 8. Automation boundary
- This PS1 does not fabricate a PKT binary or undocumented PKT XML.
- It creates the complete verified build ledger and performs targeted inspection of the installed SAC 2022 assemblies.
- A direct PKT writer is only valid after its actual SAC 2022 object model and save path are proven from the installed assemblies.
