param(
    [string]$BuilderRoot = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($BuilderRoot)) {
    $BuilderRoot = Join-Path $PSScriptRoot "..\..\..\.."
}

$BuilderRoot = (Resolve-Path -LiteralPath $BuilderRoot).Path
$errors = New-Object System.Collections.Generic.List[string]
$required = @(
    ".gitattributes",
    ".gitignore",
    "AGENTS.md",
    "README.md",
    ".agents\skills\build-unity-vr-lab-blind\SKILL.md",
    ".agents\skills\build-unity-vr-lab-blind\agents\openai.yaml",
    ".agents\skills\build-unity-vr-lab-blind\references\workflow-gates.md",
    ".agents\skills\build-unity-vr-lab-blind\references\foundation-preflight.md",
    ".agents\skills\build-unity-vr-lab-blind\references\mandatory-reuse-matrix.md",
    ".agents\skills\build-unity-vr-lab-blind\references\unity-beginner-handoff.md",
    "Tools\labflow\labflow.py",
    "Tools\create_blind_run_pack.ps1",
    "Policies\reuse-allowlist.json",
    "Policies\reuse-source-references.json",
    "Policies\gold-exclusions.json",
    "UnityProject\Packages\manifest.json",
    "UnityProject\LocalPackages\com.emvr.lab-common\package.json",
    "ApprovedAssets\EMVRRoom\Prefabs\Room_Big_Part_01.prefab",
    "ApprovedAssets\EMVRRoom\Prefabs\Room_Big_Part_01.prefab.meta",
    "ApprovedAssets\EMVRRoom\Prefabs\Room_Big_Part_02.prefab",
    "ApprovedAssets\EMVRRoom\Prefabs\Room_Big_Part_02.prefab.meta",
    "ApprovedAssets\EMVRRoom\Prefabs\Room_Big_Part_03.prefab",
    "ApprovedAssets\EMVRRoom\Prefabs\Room_Big_Part_03.prefab.meta",
    "Policies\emvr-room-assembly.json",
    "ApprovedAssets\XRInteractionToolkitSamples\Starter Assets\Prefabs\XR Interaction Setup.prefab",
    "ApprovedAssets\XRInteractionToolkitSamples\Starter Assets\Prefabs\XR Interaction Setup.prefab.meta",
    "ApprovedAssets\XRInteractionToolkitSamples\XR Device Simulator\XR Device Simulator.prefab",
    "ApprovedAssets\XRInteractionToolkitSamples\XR Device Simulator\XR Device Simulator.prefab.meta",
    "ApprovedAssets\XRInteractionToolkitSamples\XR Device Simulator\XR Device Simulator Controls.inputactions",
    "ApprovedAssets\XRInteractionToolkitSamples\XR Device Simulator\Unity.XR.Interaction.Toolkit.Samples.DeviceSimulator.asmdef",
    "ApprovedAssets",
    "KnowledgeBase",
    "KnowledgeBase\rebuild-lessons\README.md",
    "KnowledgeBase\rebuild-lessons\build-experience\README.md",
    ".agents\skills\build-unity-vr-lab-blind\scripts\import_approved_asset_closure.py",
    ".agents\skills\build-unity-vr-lab-blind\scripts\materialize_gate5_foundation.py",
    "ReusableFoundation\Gate5MinimalSlice\README.md",
    "ReusableFoundation\Gate5MinimalSlice\manifest.json",
    "LabSpecs",
    "BuildEvidence"
)

$workspaceMarkerPath = Join-Path $BuilderRoot ".emvr-workspace.json"
if (Test-Path -LiteralPath $workspaceMarkerPath) {
    try {
        $workspaceMarker = Get-Content -Raw -LiteralPath $workspaceMarkerPath | ConvertFrom-Json
        $expectedName = "EMVR_Blind_Rebuild_$($workspaceMarker.lab_id)"
        if ($workspaceMarker.schema_version -ne "1.2.0" -or
            $workspaceMarker.workflow_mode -ne "blind-rebuild" -or
            $workspaceMarker.required_master_directory_name -ne "EMVR_Blind_BuilderPack" -or
            $workspaceMarker.reference_base -ne "builder_pack_root" -or
            $workspaceMarker.path_format -ne "repository_relative_posix" -or
            $workspaceMarker.absolute_paths_persisted -ne $false -or
            (Split-Path -Leaf $BuilderRoot) -ne $expectedName -or
            (Split-Path -Leaf (Split-Path -Parent $BuilderRoot)) -ne "RebuildWorkspaces" -or
            (Split-Path -Leaf (Split-Path -Parent (Split-Path -Parent $BuilderRoot))) -ne "EMVR_Blind_BuilderPack") {
            $errors.Add("Blind rebuild workspace marker/location does not match RebuildWorkspaces/$expectedName")
        }
    }
    catch {
        $errors.Add("Invalid .emvr-workspace.json: $($_.Exception.Message)")
    }
}

foreach ($relative in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $BuilderRoot $relative))) {
        $errors.Add("Missing required Builder Pack item: $relative")
    }
}

$sourceReferenceCatalogPath = Join-Path $BuilderRoot "Policies\reuse-source-references.json"
if (Test-Path -LiteralPath $sourceReferenceCatalogPath) {
    try {
        $sourceReferenceCatalog = Get-Content -Raw -LiteralPath $sourceReferenceCatalogPath | ConvertFrom-Json
        if ($sourceReferenceCatalog.schema_version -ne "1.0.0" -or
            $sourceReferenceCatalog.reference_base -ne "builder_pack_root" -or
            $sourceReferenceCatalog.path_format -ne "repository_relative_posix" -or
            $sourceReferenceCatalog.absolute_paths_persisted -ne $false -or
            $sourceReferenceCatalog.source_access -ne "read_only") {
            $errors.Add("Reusable source-reference catalog has invalid portability/access metadata")
        }
        $sourceIds = @($sourceReferenceCatalog.sources | ForEach-Object { [string]$_.source_id })
        foreach ($requiredSourceId in @(
            'common_package',
            'gate5_foundation',
            'approved_assets',
            'mandatory_room',
            'xr_interaction_setup',
            'xr_device_simulator',
            'conditional_reusable_code_archive'
        )) {
            if ($sourceIds -notcontains $requiredSourceId) {
                $errors.Add("Reusable source-reference catalog is missing required ID: $requiredSourceId")
            }
        }
        foreach ($source in $sourceReferenceCatalog.sources) {
            $relativeSource = [string]$source.path
            $sourceSegments = $relativeSource.Split('/')
            if ([string]::IsNullOrWhiteSpace($relativeSource) -or
                [System.IO.Path]::IsPathRooted($relativeSource) -or
                $relativeSource.Contains('\') -or
                $sourceSegments -contains '.' -or
                $sourceSegments -contains '..' -or
                $source.access -ne "read_only") {
                $errors.Add("Invalid reusable source reference: $relativeSource")
                continue
            }
            if (-not (Test-Path -LiteralPath (Join-Path $BuilderRoot ($relativeSource.Replace('/', '\'))))) {
                $errors.Add("Missing reusable source reference target: $relativeSource")
            }
        }
    }
    catch {
        $errors.Add("Invalid Policies/reuse-source-references.json: $($_.Exception.Message)")
    }
}

$approvedRoomPath = Join-Path $BuilderRoot "ApprovedAssets\EMVRRoom\Prefabs\Room_Big_Part_01.prefab"
if (Test-Path -LiteralPath $approvedRoomPath) {
    $approvedRoomYaml = Get-Content -Raw -LiteralPath $approvedRoomPath
    $roomMeshRendererCount = [regex]::Matches($approvedRoomYaml, '(?m)^MeshRenderer:$').Count
    foreach ($requiredGeometry in @('Floor', 'Wall', 'Roof')) {
        if ($approvedRoomYaml -notmatch "(?m)^\s*m_Name:\s*.*$requiredGeometry") {
            $errors.Add("Mandatory EMVR room has no $requiredGeometry geometry: ApprovedAssets/EMVRRoom/Prefabs/Room_Big_Part_01.prefab")
        }
    }
    if ($roomMeshRendererCount -lt 6) {
        $errors.Add("Mandatory EMVR room is not a complete room shell; expected at least 6 MeshRenderers but found $roomMeshRendererCount.")
    }
}

$attributesPath = Join-Path $BuilderRoot ".gitattributes"
if (Test-Path -LiteralPath $attributesPath) {
    $hasBytePreservingRule = $false
    $hasTransformingRule = $false
    foreach ($line in Get-Content -LiteralPath $attributesPath) {
        $rule = $line.Split('#')[0].Trim()
        if ([string]::IsNullOrWhiteSpace($rule)) { continue }
        $tokens = @($rule -split '\s+')
        if ($tokens.Count -eq 2 -and $tokens[0] -eq '*' -and $tokens[1] -eq '-text') {
            $hasBytePreservingRule = $true
        }
        if ($tokens.Count -lt 2) { $hasTransformingRule = $true }
        foreach ($attribute in ($tokens | Select-Object -Skip 1)) {
            if ($attribute -notin @('-text', 'binary')) { $hasTransformingRule = $true }
        }
    }
    if (-not $hasBytePreservingRule -or $hasTransformingRule) {
        $errors.Add(".gitattributes must preserve '* -text' without content-transforming overrides so frozen hashes survive Git checkout and archives.")
    }
}

$xrControlsPath = Join-Path $BuilderRoot "ApprovedAssets\XRInteractionToolkitSamples\XR Device Simulator\XR Device Simulator Controls.inputactions"
if (Test-Path -LiteralPath $xrControlsPath) {
    $xrControls = Get-Content -Raw -LiteralPath $xrControlsPath
    foreach ($requiredBinding in @('"name": "Keyboard Y Translate"', '"path": "<Keyboard>/downArrow"', '"path": "<Keyboard>/upArrow"')) {
        if (-not $xrControls.Contains($requiredBinding)) {
            $errors.Add("Validated XR Device Simulator control binding is missing: $requiredBinding")
        }
    }
}

$gate5ManifestPath = Join-Path $BuilderRoot "ReusableFoundation\Gate5MinimalSlice\manifest.json"
if (Test-Path -LiteralPath $gate5ManifestPath) {
    $gate5Manifest = Get-Content -Raw -LiteralPath $gate5ManifestPath | ConvertFrom-Json
    $gate5SourceRoot = Join-Path $BuilderRoot "ReusableFoundation\Gate5MinimalSlice\Source"
    if (@($gate5Manifest.files).Count -eq 0) {
        $errors.Add("Gate 5 reusable foundation manifest contains no source files.")
    }
    $listedGate5Files = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in @($gate5Manifest.files)) {
        $sourcePath = [System.IO.Path]::GetFullPath((Join-Path $gate5SourceRoot ([string]$entry.path).Replace('/', '\')))
        $sourceRootFull = [System.IO.Path]::GetFullPath($gate5SourceRoot) + [System.IO.Path]::DirectorySeparatorChar
        if (-not $sourcePath.StartsWith($sourceRootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
            $errors.Add("Gate 5 foundation manifest path escapes Source: $($entry.path)")
        }
        elseif (-not (Test-Path -LiteralPath $sourcePath)) {
            $errors.Add("Missing Gate 5 foundation source: $($entry.path)")
        }
        else {
            if (-not $listedGate5Files.Add($sourcePath)) {
                $errors.Add("Gate 5 foundation manifest contains a duplicate path: $($entry.path)")
            }
            $actualHash = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash
            if ($actualHash -ne [string]$entry.sha256) {
                $errors.Add("Gate 5 foundation source hash mismatch: $($entry.path)")
            }
        }
    }
    if (Test-Path -LiteralPath $gate5SourceRoot) {
        foreach ($sourceFile in @(Get-ChildItem -LiteralPath $gate5SourceRoot -Recurse -File | Where-Object { $_.Extension -ne '.meta' })) {
            if (-not $listedGate5Files.Contains($sourceFile.FullName)) {
                $errors.Add("Gate 5 foundation source is not listed in manifest: $($sourceFile.FullName.Substring($gate5SourceRoot.Length).TrimStart('\'))")
            }
        }
    }
}

$oldAgentBundlePath = Join-Path $BuilderRoot "KnowledgeBase\EMVR-Agent-Knowledge"
if (Test-Path -LiteralPath $oldAgentBundlePath) {
    $errors.Add("Unsafe legacy Agent bundle remains active: KnowledgeBase\EMVR-Agent-Knowledge")
}

$restrictedArchive = Join-Path $BuilderRoot "KnowledgeBase\legacy-lab-specific-summaries"
if ((Test-Path -LiteralPath $restrictedArchive) -and -not (Test-Path -LiteralPath (Join-Path $restrictedArchive "README.md"))) {
    $errors.Add("Restricted legacy archive exists without its boundary README.")
}

$activeExampleContracts = @(Get-ChildItem -LiteralPath (Join-Path $BuilderRoot "LabInterfaceContracts") -Filter "*.example.json" -File -ErrorAction SilentlyContinue)
if ($activeExampleContracts.Count -gt 0) {
    $errors.Add("Legacy example contracts remain in the active LabInterfaceContracts directory.")
}

$activeStates = @(
    Get-ChildItem -LiteralPath (Join-Path $BuilderRoot "LabSpecs") -Recurse -Filter "workflow_state.json" -File -ErrorAction SilentlyContinue
)
if ($activeStates.Count -gt 1) {
    $errors.Add("More than one active Lab workflow exists. Use one isolated Builder Pack per run.")
}

if (Test-Path -LiteralPath (Join-Path $BuilderRoot "workflow rebuild")) {
    $errors.Add("Unexpected nested Unity project remains at `workflow rebuild`; remove or archive it outside the Builder Pack.")
}

foreach ($maintenanceLeak in @(
    "WORKFLOW_MAINTAINER_GUIDE.md",
    "WorkflowMaintenance",
    "EMVR_Workflow_Maintenance"
)) {
    if (Test-Path -LiteralPath (Join-Path $BuilderRoot $maintenanceLeak)) {
        $errors.Add("Maintainer-only material leaked into the Builder Pack: $maintenanceLeak")
    }
}

$generatedUnityPattern = '\\(Library|Temp|Logs|obj)\\'
$restrictedArchivePattern = '\\KnowledgeBase\\legacy-lab-specific-summaries\\'
$disabledIntegrationPattern = '\\OptionalIntegrations\\'
$maintenancePattern = '\\(WorkflowMaintenance|EMVR_Workflow_Maintenance)\\|\\WORKFLOW_MAINTAINER_GUIDE\.md$'
$sourceCandidates = @(
    foreach ($topLevelItem in Get-ChildItem -LiteralPath $BuilderRoot -Force -ErrorAction SilentlyContinue) {
        if ($topLevelItem.Name -in @(".git", "RebuildWorkspaces", "tmp")) { continue }
        if ($topLevelItem.PSIsContainer) {
            Get-ChildItem -LiteralPath $topLevelItem.FullName -Recurse -File -Force -ErrorAction SilentlyContinue
        }
        else {
            $topLevelItem
        }
    }
)
$sourceFiles = @(
    $sourceCandidates |
        Where-Object {
            $_.FullName -notmatch $generatedUnityPattern -and
            $_.FullName -notmatch $restrictedArchivePattern -and
            $_.FullName -notmatch $disabledIntegrationPattern -and
            $_.FullName -notmatch $maintenancePattern
        }
)

$manifestPath = Join-Path $BuilderRoot "UnityProject\Packages\manifest.json"
if (Test-Path -LiteralPath $manifestPath) {
    $manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
    $manifestDirectory = Split-Path -Parent $manifestPath
    $lockPath = Join-Path $manifestDirectory "packages-lock.json"
    $lock = $null
    if (-not (Test-Path -LiteralPath $lockPath)) {
        $errors.Add("Unity packages-lock.json is missing.")
    }
    else {
        $lock = Get-Content -Raw -LiteralPath $lockPath | ConvertFrom-Json
    }

    foreach ($package in $manifest.dependencies.PSObject.Properties) {
        $version = [string]$package.Value
        if ($version.StartsWith("file:", [System.StringComparison]::OrdinalIgnoreCase)) {
            $relativePath = $version.Substring(5).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
            $resolvedPath = [System.IO.Path]::GetFullPath((Join-Path $manifestDirectory $relativePath))
            if (-not (Test-Path -LiteralPath (Join-Path $resolvedPath "package.json"))) {
                $errors.Add("Local UPM dependency does not resolve to package.json: $($package.Name) -> $version -> $resolvedPath")
            }

            if ($null -ne $lock) {
                $lockEntry = $lock.dependencies.PSObject.Properties[$package.Name]
                if ($null -eq $lockEntry) {
                    $errors.Add("packages-lock.json has no entry for local dependency: $($package.Name)")
                }
                elseif ([string]$lockEntry.Value.version -ne $version) {
                    $errors.Add("Local dependency path differs between manifest and lock: $($package.Name)")
                }
            }
        }
    }

    $dependency = $manifest.dependencies.'com.emvr.lab-common'
    if ($dependency -ne "file:../LocalPackages/com.emvr.lab-common") {
        $errors.Add("com.emvr.lab-common must use file:../LocalPackages/com.emvr.lab-common.")
    }
    else {
        $resolvedCommon = [System.IO.Path]::GetFullPath((Join-Path $manifestDirectory $dependency.Substring(5).Replace('/', [System.IO.Path]::DirectorySeparatorChar)))
        $expectedCommon = [System.IO.Path]::GetFullPath((Join-Path $BuilderRoot "UnityProject\LocalPackages\com.emvr.lab-common"))
        if (-not $resolvedCommon.Equals($expectedCommon, [System.StringComparison]::OrdinalIgnoreCase)) {
            $errors.Add("com.emvr.lab-common resolves outside the canonical UnityProject/LocalPackages location: $resolvedCommon")
        }
    }

    $mcpDependency = $manifest.dependencies.'com.ivanmurzak.unity.mcp'
    if (-not [string]::IsNullOrWhiteSpace($mcpDependency)) {
        $errors.Add("Unity MCP is disabled by Builder Pack policy but remains in manifest.json.")
    }
}

$projectSettingsPath = Join-Path $BuilderRoot "UnityProject\ProjectSettings\ProjectSettings.asset"
if (Test-Path -LiteralPath $projectSettingsPath) {
    if (Select-String -LiteralPath $projectSettingsPath -Pattern "UNITY_MCP_READY" -SimpleMatch -Quiet) {
        $errors.Add("UNITY_MCP_READY is present while Unity MCP is disabled.")
    }
}

$activeMcpDllPath = Join-Path $BuilderRoot "UnityProject\Assets\Plugins\NuGet"
if (Test-Path -LiteralPath $activeMcpDllPath) {
    $errors.Add("Unity MCP NuGet DLLs remain inside UnityProject/Assets while MCP is disabled.")
}

function Get-DirectoryDigest([string]$Path) {
    $lines = [System.Collections.Generic.List[string]]::new()
    foreach ($file in @(Get-ChildItem -LiteralPath $Path -Recurse -File)) {
        $relative = $file.FullName.Substring($Path.Length).TrimStart('\').Replace('\', '/')
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        $lines.Add("$relative`t$hash")
    }
    $lines.Sort([System.StringComparer]::Ordinal)
    $bytes = [System.Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '')
    }
    finally {
        $sha.Dispose()
    }
}

$canonicalCommon = Join-Path $BuilderRoot "UnityProject\LocalPackages\com.emvr.lab-common"
$legacyCommonMirror = Join-Path $BuilderRoot "LocalPackages\com.emvr.lab-common"
if ((Test-Path -LiteralPath $canonicalCommon) -and (Test-Path -LiteralPath $legacyCommonMirror)) {
    $canonicalHash = Get-DirectoryDigest $canonicalCommon
    $mirrorHash = Get-DirectoryDigest $legacyCommonMirror
    if ($canonicalHash -ne $mirrorHash) {
        $errors.Add("Duplicate Common packages have drifted. The user must synchronize or remove the legacy root mirror.")
    }
}

$inventoryPath = Join-Path $BuilderRoot "Policies\pack-inventory.json"
if (Test-Path -LiteralPath $inventoryPath) {
    $inventory = Get-Content -Raw -LiteralPath $inventoryPath | ConvertFrom-Json
    $roomRecipePath = Join-Path $BuilderRoot "Policies\emvr-room-assembly.json"
    if ((Test-Path -LiteralPath $roomRecipePath) -and
        (Get-FileHash -LiteralPath $roomRecipePath -Algorithm SHA256).Hash -ne $inventory.sections.EMVRRoomAssembly.sha256) {
        $errors.Add("EMVRLABS room assembly recipe differs from the frozen inventory.")
    }
    $commonInventory = $inventory.sections.'UnityProject/LocalPackages/com.emvr.lab-common'
    if ($inventory.sections.EMVRRoomLighting) {
        $lightingProfilePath = Join-Path $BuilderRoot 'Policies/emvr-room-lighting.json'
        if (-not (Test-Path -LiteralPath $lightingProfilePath) -or
            (Get-FileHash -LiteralPath $lightingProfilePath -Algorithm SHA256).Hash -ne $inventory.sections.EMVRRoomLighting.sha256) {
            $errors.Add('Full room lighting profile is missing or differs from frozen inventory.')
        } else {
            $lightingProfile = Get-Content -Raw -LiteralPath $lightingProfilePath | ConvertFrom-Json
            foreach ($entry in $lightingProfile.source_files) {
                $lightingSourcePath = Join-Path $BuilderRoot $entry.path
                if (-not (Test-Path -LiteralPath $lightingSourcePath) -or
                    (Get-FileHash -LiteralPath $lightingSourcePath -Algorithm SHA256).Hash -ne $entry.sha256) {
                    $errors.Add("Read-only lighting source/archive changed: $($entry.path)")
                }
                if ($entry.path -like '*.lighting' -and (Test-Path -LiteralPath $lightingSourcePath)) {
                    if ([System.IO.File]::ReadAllBytes($lightingSourcePath) -contains 13) {
                        $errors.Add('Published RoomLighting must use LF line endings for Unity serialization; maintainer repair required.')
                    }
                }
            }
        }
    }
    $commonPackageJson = Join-Path $canonicalCommon "package.json"
    if ((Test-Path -LiteralPath $commonPackageJson) -and $commonInventory.package_json_sha256) {
        $actualPackageHash = (Get-FileHash -LiteralPath $commonPackageJson -Algorithm SHA256).Hash
        if ($actualPackageHash -ne $commonInventory.package_json_sha256) {
            $errors.Add("Canonical Common package.json differs from Policies/pack-inventory.json.")
        }
    }
    if (Test-Path -LiteralPath $canonicalCommon) {
        $commonFiles = @(Get-ChildItem -LiteralPath $canonicalCommon -Recurse -File)
        $commonBytes = ($commonFiles | Measure-Object -Property Length -Sum).Sum
        if ($commonFiles.Count -ne [int]$commonInventory.files -or $commonBytes -ne [long]$commonInventory.bytes) {
            $errors.Add("Canonical Common file count/size differs from the frozen inventory.")
        }
        if ($commonInventory.directory_digest_sha256 -and (Get-DirectoryDigest $canonicalCommon) -ne [string]$commonInventory.directory_digest_sha256) {
            $errors.Add("Canonical Common content digest differs from the frozen inventory.")
        }
    }
    $approvedRoot = Join-Path $BuilderRoot "ApprovedAssets"
    $approvedInventory = $inventory.sections.ApprovedAssets
    if (Test-Path -LiteralPath $approvedRoot) {
        $approvedFiles = @(Get-ChildItem -LiteralPath $approvedRoot -Recurse -File)
        $approvedBytes = ($approvedFiles | Measure-Object -Property Length -Sum).Sum
        if ($approvedFiles.Count -ne [int]$approvedInventory.files -or $approvedBytes -ne [long]$approvedInventory.bytes) {
            $errors.Add("ApprovedAssets file count/size differs from the frozen inventory.")
        }
        if ($approvedInventory.directory_digest_sha256 -and (Get-DirectoryDigest $approvedRoot) -ne [string]$approvedInventory.directory_digest_sha256) {
            $errors.Add("ApprovedAssets content digest differs from the frozen inventory.")
        }
    }
}

$forbiddenFileNames = @(
    "ElectricField.cs",
    "MagneticField.cs",
    "WaveField.cs",
    "ChargingBox.cs",
    "Solenoid.cs",
    "Polarizer.cs",
    "QuarterPlate.cs",
    "Game_1_P.cs",
    "Game_1_S.cs",
    "Game_1_W.cs",
    "Game_2_P.cs",
    "Game_2_S.cs",
    "Game_2_W.cs",
    "Game_3_P.cs",
    "Game_3_S.cs",
    "Game_3_W.cs",
    "Lab1ElectrostaticsRebuildSceneBuilder.cs"
)

$presentNames = $sourceFiles | Where-Object { $forbiddenFileNames -contains $_.Name }
foreach ($file in $presentNames) {
    $errors.Add("Gold implementation file present: $($file.FullName.Substring($BuilderRoot.Length + 1))")
}

$forbiddenScenes = @(
    "Static_Electric_Field.unity",
    "Static_Magnetic_Field.unity",
    "Wave_Polarization.unity",
    "Lab1_Electrostatics_Rebuild.unity"
)
$presentScenes = $sourceFiles | Where-Object {
    $_.Extension -eq ".unity" -and $forbiddenScenes -contains $_.Name
}
foreach ($scene in $presentScenes) {
    $errors.Add("Gold scene present: $($scene.FullName.Substring($BuilderRoot.Length + 1))")
}

if ($errors.Count -gt 0) {
    Write-Host "[EMVR Builder Boundary] FAIL" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "- $_" -ForegroundColor Red }
    exit 1
}

$commonPackage = Get-Content -Raw -LiteralPath (Join-Path $BuilderRoot "UnityProject\LocalPackages\com.emvr.lab-common\package.json") | ConvertFrom-Json
$fileCount = $sourceFiles.Count
Write-Host "[EMVR Builder Boundary] PASS" -ForegroundColor Green
Write-Host "Builder root: $BuilderRoot"
Write-Host "Common version: $($commonPackage.version)"
Write-Host "Files checked: $fileCount"
