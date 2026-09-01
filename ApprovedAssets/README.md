# Approved assets

These assets were copied from the frozen workflow baseline and are approved for
reuse. They include models, Oculus hand assets, XR/XRI settings, XRI Starter
Assets, XR Device Simulator assets, materials, audio, TextMesh Pro resources,
and general prefabs.

The mandatory room source is
`EMVRRoom/Prefabs/Room_Big_Part_01.prefab`. Its approved GUID closure under
`EMVRRoom/Meshes`, `EMVRRoom/Materials`, and `EMVRRoom/Textures` is extracted
from the previously validated generic modular room. It contains no Lab formula,
answer, step logic, controller script, or donor scene. The XRI
`Teleportation Environment.prefab` remains an optional sample ground and is not
an enclosed room.

Import only the selected asset and its in-`ApprovedAssets` GUID dependency
closure with the Skill's `import_approved_asset_closure.py`. The fixed current-
run destination is
`UnityProject/Assets/Generated/<lab_id>/ApprovedDependencies/`; preserve all
`.meta` companions and the generated manifest under
`BuildEvidence/<lab_id>/approved-asset-imports/`. Summarize the approved import
in `BuildEvidence/<lab_id>/reuse-report.md`.

Asset approval is visual/runtime-infrastructure approval only. Prefab scripts or
old Lab behavior are not implicitly approved. If a prefab contains a missing or
Lab-specific component, remove that component through a new Editor builder and
wire it to approved Common or new Lab code.
