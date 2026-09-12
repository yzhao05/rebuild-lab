param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[a-z][a-z0-9_]{2,63}$')]
    [string]$LabId,
    [string]$SourceRoot = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $SourceRoot = Join-Path $PSScriptRoot ".."
}

$SourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
if ((Split-Path -Leaf $SourceRoot) -ne "EMVR_Blind_BuilderPack") {
    throw "Blind rebuild source root must be the master directory named EMVR_Blind_BuilderPack: $SourceRoot"
}
$workspaceContainer = Join-Path $SourceRoot "RebuildWorkspaces"
$Destination = Join-Path $workspaceContainer ("EMVR_Blind_Rebuild_" + $LabId)

if (Test-Path -LiteralPath $Destination) {
    throw "The fixed rebuild workspace already exists; resume it or remove it after preserving its results: $Destination"
}

function Convert-ToExtendedLengthPath([string]$Path) {
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if ($fullPath.StartsWith('\\?\')) { return $fullPath }
    if ($fullPath.StartsWith('\\')) { return '\\?\UNC\' + $fullPath.Substring(2) }
    return '\\?\' + $fullPath
}

function New-PortableDirectory([string]$Path) {
    [System.IO.Directory]::CreateDirectory((Convert-ToExtendedLengthPath $Path)) | Out-Null
}

function Copy-PortableFile([string]$Source, [string]$Target) {
    $extendedSource = Convert-ToExtendedLengthPath $Source
    $extendedTarget = Convert-ToExtendedLengthPath $Target
    [System.IO.File]::Copy($extendedSource, $extendedTarget, $false)
}

function Test-RunOutput([string]$Relative) {
    $normalized = $Relative.Replace('/', '\')
    $parent = Split-Path -Parent $normalized
    $extension = [System.IO.Path]::GetExtension($normalized).ToLowerInvariant()
    if ($normalized -like ".git\*" -or
        $normalized -like "RebuildWorkspaces\*" -or
        $normalized -like "tmp\*") { return $true }
    if ($parent -eq "UnityProject" -and
        $extension -in @(".csproj", ".sln", ".userprefs")) { return $true }
    if ($normalized -eq "WORKFLOW_MAINTAINER_GUIDE.md" -or
        $normalized -like "WorkflowMaintenance\*" -or
        $normalized -like "EMVR_Workflow_Maintenance\*") { return $true }
    if ($normalized -like "KnowledgeBase\legacy-lab-specific-summaries\*") { return $true }
    if ($normalized -like "LabSpecs\*") {
        return -not ($normalized -eq "LabSpecs\README.md" -or $normalized -like "LabSpecs\templates\*")
    }
    if ($normalized -like "LabInterfaceContracts\*") {
        return $normalized -ne "LabInterfaceContracts\README.md"
    }
    if ($normalized -like "BuildEvidence\*") {
        return -not ($normalized -like "BuildEvidence\EvalTemplates\*")
    }
    foreach ($prefix in @(
        "UnityProject\Assets\Editor\",
        "UnityProject\Assets\Generated\",
        "UnityProject\Assets\Scenes\",
        "UnityProject\Assets\Scripts\",
        "UnityProject\Assets\Tests\"
    )) {
        if ($normalized.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) { return $true }
    }
    if ($normalized -like "UnityProject\Library\*" -or
        $normalized -like "UnityProject\Temp\*" -or
        $normalized -like "UnityProject\Logs\*" -or
        $normalized -like "UnityProject\obj\*" -or
        $normalized -like "UnityProject\UserSettings\*" -or
        $normalized -like "workflow rebuild\*") { return $true }
    if ($normalized -like "*\__pycache__\*" -or $normalized -like "*.pyc") { return $true }
    return $false
}

$filesToCopy = @(
    foreach ($topLevelItem in Get-ChildItem -LiteralPath $SourceRoot -Force) {
        if ($topLevelItem.Name -in @(".git", "RebuildWorkspaces", "tmp")) { continue }
        $candidates = if ($topLevelItem.PSIsContainer) {
            Get-ChildItem -LiteralPath $topLevelItem.FullName -Recurse -File -Force
        }
        else {
            @($topLevelItem)
        }
        foreach ($candidate in $candidates) {
            $relative = $candidate.FullName.Substring($SourceRoot.Length + 1)
            if (-not (Test-RunOutput $relative)) { $candidate }
        }
    }
)

New-PortableDirectory $workspaceContainer
New-PortableDirectory $Destination
try {
    $copied = 0
    $filesToCopy | ForEach-Object {
        $relative = $_.FullName.Substring($SourceRoot.Length + 1)
        $target = Join-Path $Destination $relative
        $targetParent = Split-Path -Parent $target
        New-PortableDirectory $targetParent
        Copy-PortableFile $_.FullName $target
        $copied++
    }

    foreach ($relativeDirectory in @(
        "BuildEvidence",
        "LabInterfaceContracts",
        "LabSpecs",
        "UnityProject\Assets\Editor",
        "UnityProject\Assets\Generated",
        "UnityProject\Assets\Scenes",
        "UnityProject\Assets\Scripts",
        "UnityProject\Assets\Tests"
    )) {
        New-PortableDirectory (Join-Path $Destination $relativeDirectory)
    }

    $marker = [ordered]@{
        schema_version = "1.2.0"
        workflow_mode = "blind-rebuild"
        lab_id = $LabId
        required_master_directory_name = "EMVR_Blind_BuilderPack"
        required_directory_name = "EMVR_Blind_Rebuild_$LabId"
        reference_base = "builder_pack_root"
        path_format = "repository_relative_posix"
        absolute_paths_persisted = $false
    } | ConvertTo-Json
    Set-Content -LiteralPath (Join-Path $Destination ".emvr-workspace.json") -Value $marker -Encoding utf8

    $verifier = Join-Path $Destination ".agents\skills\build-unity-vr-lab-blind\scripts\verify_builder_pack.ps1"
    & $verifier -BuilderRoot $Destination
    if (-not $?) {
        throw "The copied rebuild workspace failed Builder Boundary verification."
    }
    Write-Host "[EMVR Blind Rebuild Workspace] CREATED" -ForegroundColor Green
    Write-Host "Workspace: $Destination"
    Write-Host "Lab ID: $LabId"
    Write-Host "Files copied: $copied"
    Write-Host "Open this exact child directory as the next Codex workspace."
}
catch {
    Write-Host "Rebuild workspace creation failed. Partial destination remains for inspection: $Destination" -ForegroundColor Red
    throw
}
