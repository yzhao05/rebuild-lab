param(
    [Parameter(Mandatory = $true)]
    [string]$Destination,
    [string]$SourceRoot = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $SourceRoot = Join-Path $PSScriptRoot ".."
}

$SourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
$destinationParent = Split-Path -Parent ([System.IO.Path]::GetFullPath($Destination))
if (-not (Test-Path -LiteralPath $destinationParent)) {
    throw "Destination parent does not exist: $destinationParent"
}
$Destination = [System.IO.Path]::GetFullPath($Destination)
if ($Destination.Equals($SourceRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
    $Destination.StartsWith($SourceRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Destination must be outside the source Builder Pack."
}
if (Test-Path -LiteralPath $Destination) {
    throw "Destination already exists; refusing to overwrite: $Destination"
}

function Test-RunOutput([string]$Relative) {
    $normalized = $Relative.Replace('/', '\')
    $parent = Split-Path -Parent $normalized
    $extension = [System.IO.Path]::GetExtension($normalized).ToLowerInvariant()
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
        $normalized -like "tmp\*" -or
        $normalized -like "workflow rebuild\*") { return $true }
    if ($normalized -like "*\__pycache__\*" -or $normalized -like "*.pyc") { return $true }
    return $false
}

New-Item -ItemType Directory -Path $Destination | Out-Null
try {
    $copied = 0
    Get-ChildItem -LiteralPath $SourceRoot -Recurse -File -Force | ForEach-Object {
        $relative = $_.FullName.Substring($SourceRoot.Length + 1)
        if (Test-RunOutput $relative) { return }
        $target = Join-Path $Destination $relative
        $targetParent = Split-Path -Parent $target
        if (-not (Test-Path -LiteralPath $targetParent)) {
            New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
        }
        Copy-Item -LiteralPath $_.FullName -Destination $target
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
        New-Item -ItemType Directory -Path (Join-Path $Destination $relativeDirectory) -Force | Out-Null
    }

    $verifier = Join-Path $Destination ".agents\skills\build-unity-vr-lab-blind\scripts\verify_builder_pack.ps1"
    & $verifier -BuilderRoot $Destination
    if (-not $?) {
        throw "The copied run Pack failed Builder Boundary verification."
    }
    Write-Host "[EMVR Blind Run Pack] CREATED" -ForegroundColor Green
    Write-Host "Destination: $Destination"
    Write-Host "Files copied: $copied"
    Write-Host "Restricted legacy material and prior-run output: excluded"
}
catch {
    Write-Host "Run Pack creation failed. Partial destination remains for inspection: $Destination" -ForegroundColor Red
    throw
}
