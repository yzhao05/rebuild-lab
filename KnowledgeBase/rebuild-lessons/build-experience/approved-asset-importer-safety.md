---
knowledge_kind: rebuild_lessons
source_run_type: workflow_maintenance
source_scope: approved_asset_importer
verified: true
captured_on: 2026-08-14
---

# Approved asset importer path, assembly, and transaction safety

This is a cross-Lab construction safeguard. It contains no target-Lab
requirements, answers, coordinates, class names, or scene wiring.

## Verified failure pattern

- A Unity asset path can be safely below Unity's project-relative limit while
  its absolute path exceeds the legacy Windows `MAX_PATH` limit because blind
  rebuilds live inside a named child workspace.
- Passing that absolute path directly to ordinary filesystem copy calls can
  raise `FileNotFoundError: [WinError 3]` when Windows long paths are disabled.
- Preflighting source hashes and destination conflicts is not sufficient by
  itself. A runtime copy failure can still leave a valid-looking subset unless
  the importer owns a transaction and commits provenance last.
- A prefab GUID closure can pull in C# files without directly referencing their
  controlling `.asmdef` or `.asmref`. Copying those scripts alone moves them
  into the default project assembly, which can change namespace visibility,
  `InternalsVisibleTo` access, define constraints, and package references even
  though every copied script hash is correct.

## Required prevention behavior

- Keep the Unity-relative destination and every `.meta` companion unchanged,
  but use Windows extended-length paths for host filesystem operations.
- Apply the same extended-length-path rule to Builder Pack workspace creation;
  `create_blind_run_pack.ps1` must not use ordinary `Copy-Item`/`New-Item`
  calls for deep Unity asset destinations.
- Precompute and validate the complete closure before the first write.
- Verify the hash immediately after every copy.
- Write the import manifest atomically only after all files verify.
- Make every state/readiness validator that consumes the manifest use the same
  long-path-safe existence, hashing, and recursive-enumeration behavior; a
  successful import followed by a false "copy is missing" blocker is still a
  workflow defect.
- On failure, remove all uncommitted files created or adopted for that exact
  closure and leave mismatched/conflicting files untouched. Before rollback,
  audit every other committed import manifest for the current Lab; never
  remove a shared destination or `.meta` owned by another closure.
- A retry may adopt pre-existing files only when they match the approved source
  hash exactly; the successful retry must still produce the complete manifest.
- For every imported `.cs`, walk upward inside `ApprovedAssets` and include its
  nearest Unity assembly boundary (`.asmdef` or `.asmref`) with the matching
  `.meta`. Parse `GUID:` assembly references case-insensitively so an `.asmref`
  also brings in its referenced approved `.asmdef`.
- An existing committed manifest may be expanded after an importer repair only
  when its source, Lab, destination, mode, and every prior asset record are
  unchanged. Copy and hash-check the new boundary files before atomically
  replacing the manifest; never use this rule to alter or remove an old record.

## Ownership and Gate action

The importer is protected workflow foundation. A Builder Agent must report the
first failure and must not shorten paths, hand-copy dependencies, delete
partial files, edit the importer, or continue Gate 5. A workflow maintainer
repairs and verifies the importer; the Builder Agent then reruns the same
controlled Gate 5 command.

## Verified XR baseline

The validated portable XR entry is the approved `XR Interaction Setup.prefab`
plus `XR Device Simulator.prefab`, not a bare `XR Origin` instantiated as the
top-level rig. Import both dependency closures so the Starter Assets and Device
Simulator `.asmdef` files remain present. The approved simulator controls must
retain the Up/Down-arrow `Keyboard Y Translate` bindings verified in Lab1.
Configure controller rays through Common `EmVrXrRayRuntimeTuner` and
`EmVrXrRayUtility`; do not copy Lab1-named scripts or patch imported output.

## Search cues

`WinError 3`, `LongPathsEnabled=0`, `destination path 260`, `partial approved
asset import`, `missing approved-asset import manifest`, `CS0122`,
`ComponentLocatorUtility`, `InternalsVisibleTo`, `script imported without
asmdef`, `Gate 5 foundation defect`.
