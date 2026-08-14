param(
    [string]$BuilderRoot = "",
    [switch]$IncludeLegacyMirror,
    [switch]$IncludeDisabledMcp
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($BuilderRoot)) {
    $BuilderRoot = Join-Path $PSScriptRoot "..\..\..\.."
}

$BuilderRoot = (Resolve-Path -LiteralPath $BuilderRoot).Path
$codeExtensions = @(".cs", ".py", ".ps1", ".sh", ".bat", ".cmd", ".js", ".ts")
$generatedUnityPattern = '\\(Library|Temp|Logs|obj)\\'

$files = foreach ($topLevelItem in Get-ChildItem -LiteralPath $BuilderRoot -Force -ErrorAction SilentlyContinue) {
    if ($topLevelItem.Name -in @(".git", "RebuildWorkspaces", "tmp")) { continue }
    $candidates = if ($topLevelItem.PSIsContainer) {
        Get-ChildItem -LiteralPath $topLevelItem.FullName -Recurse -File -ErrorAction SilentlyContinue
    }
    else {
        @($topLevelItem)
    }
    $candidates | Where-Object {
        $codeExtensions -contains $_.Extension.ToLowerInvariant() -and
        $_.FullName -notmatch $generatedUnityPattern
    }
}

$inventory = foreach ($file in $files) {
    $relative = $file.FullName.Substring($BuilderRoot.Length + 1)

    if ($relative -like "LocalPackages\com.emvr.lab-common\*") {
        if (-not $IncludeLegacyMirror) { continue }
        $category = "Common legacy mirror"
    }
    elseif ($relative -like "OptionalIntegrations\UnityMCP\*") {
        if (-not $IncludeDisabledMcp) { continue }
        $category = "Disabled Unity MCP"
    }
    elseif ($relative -like "OptionalIntegrations\LabMCP\*") {
        if (-not $IncludeDisabledMcp) { continue }
        $category = "Disabled Lab MCP"
    }
    elseif ($relative -like "KnowledgeBase\legacy-lab-specific-summaries\*") {
        continue
    }
    elseif ($relative -like "UnityProject\LocalPackages\com.emvr.lab-common\*") {
        $category = "Common canonical"
    }
    elseif ($relative -like "KnowledgeBase\ReusableCodeArchive\*") {
        $category = "Conditional reusable archive"
    }
    elseif ($relative -like "ReusableFoundation\*") {
        $category = "Protected reusable foundation"
    }
    elseif ($relative -like "KnowledgeBase\AgentTools\*") {
        $category = "Knowledge/agent tool archive"
    }
    elseif ($relative -like "ApprovedAssets\*") {
        $category = "Approved third-party asset dependency"
    }
    elseif ($relative -like "UnityProject\Assets\*") {
        $category = "Current-run Lab implementation"
    }
    elseif ($relative -like "Tools\*" -or $relative -like ".agents\*") {
        $category = "Workflow implementation"
    }
    else {
        $category = "Other"
    }

    [pscustomobject]@{
        Category = $category
        Path = $relative.Replace('\', '/')
    }
}

$inventory | Sort-Object Category, Path
