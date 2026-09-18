[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$BundlePath)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[xml]$manifest = Get-Content (Join-Path $BundlePath 'PackageContents.xml') -Raw
if ($manifest.ApplicationPackage.AppVersion -ne '0.9.0') { throw 'Unexpected package version.' }
$runtime = $manifest.ApplicationPackage.Components.RuntimeRequirements
if ($runtime.SeriesMin -ne 'R23.1' -or $runtime.SeriesMax -ne 'R23.1' -or $runtime.OS -ne 'Win64') { throw 'Package must target AutoCAD 2020 x64.' }
$entry = $manifest.ApplicationPackage.Components.ComponentEntry
if ($entry.Commands.Command.Global -ne 'OCR2GEOMETRY' -or $entry.LoadOnCommandInvocation -ne 'True') { throw 'Command autoload is not configured.' }
$module = Join-Path $BundlePath $entry.ModuleName
$version = [Reflection.AssemblyName]::GetAssemblyName($module).Version.ToString()
if ($version -ne '0.9.0.0') { throw "Unexpected assembly version: $version" }
foreach ($file in 'Contents/Windows/Tesseract.dll','Contents/Windows/tessdata/eng.traineddata','INSTALL.md') {
    if (!(Test-Path (Join-Path $BundlePath $file))) { throw "Missing package file: $file" }
}
if ((Get-Item (Join-Path $BundlePath 'Contents/Windows/tessdata/eng.traineddata')).Length -lt 1000000) { throw 'Language model is incomplete.' }
$forbidden = @(Get-ChildItem $BundlePath -Recurse -File | Where-Object { $_.Name -match '^(acmgd|acdbmgd|accoremgd)\.dll$' })
if ($forbidden.Count) { throw 'Do not redistribute Autodesk reference DLLs.' }
$native = @(Get-ChildItem (Join-Path $BundlePath 'Contents/Windows/x64') -Filter '*.dll')
if (!($native.Name -match 'tesseract') -or !($native.Name -match 'lept')) { throw 'Missing native OCR libraries.' }
Write-Host 'Bundle validation passed.'
