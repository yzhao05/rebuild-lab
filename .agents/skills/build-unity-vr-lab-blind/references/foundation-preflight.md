# Foundation preflight

Complete this preflight before creating or changing Lab C#.

1. Run `scripts/verify_builder_pack.ps1`.
2. Resolve every `file:` dependency relative to
   `UnityProject/Packages/manifest.json`; require `package.json` at the resolved
   location.
3. Require the identical dependency string in `packages-lock.json`.
4. Require `com.emvr.lab-common` to resolve to
   `UnityProject/LocalPackages/com.emvr.lab-common`.
5. If the legacy root Common mirror exists, require its full directory hash to
   match the canonical package. A mismatch is a user-owned foundation defect.
   Also require Common and ApprovedAssets to match the full-tree SHA-256 values
   in `Policies/pack-inventory.json`; file count/size alone is insufficient.
6. Require Unity MCP, its OpenUPM registry, `UNITY_MCP_READY`, and active MCP
   NuGet DLLs under `UnityProject/Assets` to be absent.
7. Require `KnowledgeBase/rebuild-lessons/README.md`, recursively review every
   prior lesson file in that directory except files named `README.md`, with
   particular attention to `build-experience/`, and complete the current Lab's
   `prior-lessons-review.md` with applicable safeguards before Gate 0 approval.
8. Confirm that `KnowledgeBase/legacy-lab-specific-summaries/` was not searched
   or read. Its old implementation details and answers are outside blind-build
   context even though the archive is physically retained in the pack.
9. Require exactly zero active runs before `labflow new`, or exactly one active
   run with the same `lab_id` when resuming. Never expose a prior run's LabSpec,
   canonical contracts, Unity output, or evidence to a new target.
10. Require `LabInterfaceContracts/` to contain no `.example.json` files and
    `KnowledgeBase/EMVR-Agent-Knowledge/` to be absent; both belong in the
    restricted archive.
11. Require `ReusableFoundation/Gate5MinimalSlice/manifest.json` to list every
    non-`.meta` source file exactly once with a matching SHA-256. Reject any
    unlisted source file. The actual copy and approved-room import happen only
    after Gate 4 approval, during Gate 5.
12. When resuming, require `workflow_state.json` schema `1.7.0`. If `status`
    reports an older schema, run `labflow migrate-state`; all prior approvals
    and evidence must be reviewed again from Gate 0.

Do not start Lab generation when any check fails. Report the first root failure
and stop; do not edit the foundation or continue into cascaded errors.

For compile failures, diagnose in this order:

1. local UPM path and `package.json`;
2. manifest/lock agreement;
3. Common assembly compilation;
4. Lab assembly compilation.

Treat missing `EMVR.Common`, `EmVrLabObject`, `IEmVrDragObserver`,
`EmVrStatusDisplay`, and similar symbols as possible dependency-resolution
symptoms before treating them as Lab source defects. Do not remove valid Common
usings or rewrite Lab calls until their owning assembly is proven healthy.

For release verification, ask the user to open a copied Builder Pack with no
`Library` cache and report the first import/compile result. Never delete the
user's active `Library` automatically. Record only observed results.
