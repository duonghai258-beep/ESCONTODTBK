# SAC 2022 API schema probe — BTXM5

Purpose: inspect the ACTUAL installed SAC 2022 assemblies before any PKT writer is authored.

This probe does NOT create, modify, overwrite, or fabricate a PKT/XML project.

Run on the machine that has:
C:\Program Files\Autodesk\Subassembly Composer 2022

It records public constructors, properties, fields, methods and enum values for:
- Autodesk.SubassemblyComposer.FileAccess.PktFileAccess
- PktStructure
- ParamCollection / ParamItem / SubassemblyItem
- WorkflowEngine parameter/target classes
- ActivityLibrary CreatePoint/CreateLink/CreateShape
- ActivityDesigner classes
- WorkflowHost

The result is needed to map the actual SAC 2022 object graph to the already-locked BTXM5 ledger.

No other SAC/Civil 3D version is used.
