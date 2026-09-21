param(
    [Parameter(Mandatory=$true)][string]$InputDocx,
    [Parameter(Mandatory=$true)][string]$OutputPdf
)
$ErrorActionPreference = 'Stop'
$inputFull = (Resolve-Path -LiteralPath $InputDocx).Path
$outputFull = [IO.Path]::GetFullPath($OutputPdf)
if ([IO.Path]::GetExtension($inputFull) -ne '.docx' -or [IO.Path]::GetExtension($outputFull) -ne '.pdf') {
    throw 'Expected a DOCX input and a distinct PDF output.'
}
$wordInstance = $null
$documentInstance = $null
$confirmConversion = $false
$readOnly = $true
$addToRecent = $false
$doNotSave = 0
try {
    # A new hidden instance; never attach to or save the user's open documents.
    $wordInstance = New-Object -ComObject Word.Application
    $wordInstance.Visible = $false
    $wordInstance.DisplayAlerts = 0
    $wordInstance.AutomationSecurity = 3
    $documentInstance = $wordInstance.Documents.Open([ref]$inputFull, [ref]$confirmConversion, [ref]$readOnly, [ref]$addToRecent)
    $documentInstance.ExportAsFixedFormat($outputFull, 17)
} finally {
    try {
        if ($null -ne $documentInstance) {
            try { $documentInstance.Close([ref]$doNotSave) }
            finally { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($documentInstance) }
        }
    } finally {
        if ($null -ne $wordInstance) {
            try { $wordInstance.Quit([ref]$doNotSave) }
            finally { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($wordInstance) }
        }
    }
}
if (-not (Test-Path -LiteralPath $outputFull -PathType Leaf)) { throw 'PDF export did not produce a file.' }
