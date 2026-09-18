[CmdletBinding()]
param(
    [string]$Acad2020Dir = "$env:ProgramFiles\Autodesk\AutoCAD 2020",
    [string]$MSBuildPath,
    [string]$IsccPath,
    [string]$TrainedDataPath,
    [string]$VCRedistPath
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') { throw 'Build this installer on Windows with AutoCAD 2020 and Visual Studio installed.' }
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root 'artifacts'
$stage = Join-Path $out 'OCR2Geometry.bundle'
$windows = Join-Path $stage 'Contents\Windows'
$cache = Join-Path $out 'downloads'
foreach ($dll in 'acmgd.dll','acdbmgd.dll','accoremgd.dll') {
    if (!(Test-Path (Join-Path $Acad2020Dir $dll))) { throw "Missing AutoCAD 2020 reference: $dll in $Acad2020Dir" }
}
if (!$MSBuildPath) {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (!(Test-Path $vswhere)) { throw 'Install Visual Studio with .NET desktop development and the .NET Framework 4.7.2 targeting pack.' }
    $MSBuildPath = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
}
if (!$MSBuildPath -or !(Test-Path $MSBuildPath)) { throw 'MSBuild.exe was not found. Supply -MSBuildPath.' }
if (!$IsccPath) { $IsccPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" }
if (!(Test-Path $IsccPath)) { throw 'Install Inno Setup 6.3 or later, or supply -IsccPath.' }
& $MSBuildPath (Join-Path $root 'OCR2Geometry.sln') /restore /t:Rebuild /p:Configuration=Release "/p:Acad2020Dir=$Acad2020Dir" /nologo
if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed.' }
& $MSBuildPath (Join-Path $root 'tests\ParserRegression\ParserRegression.csproj') /t:Rebuild /p:Configuration=Release /nologo
if ($LASTEXITCODE -ne 0) { throw 'Regression test build failed.' }
& (Join-Path $root 'tests\ParserRegression\bin\ParserRegression.exe')
if ($LASTEXITCODE -ne 0) { throw 'Regression tests failed.' }
New-Item $cache -ItemType Directory -Force | Out-Null
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
function Download([string]$Url, [string]$Destination) {
    Invoke-WebRequest -UseBasicParsing -Uri $Url -OutFile "$Destination.partial"
    Move-Item "$Destination.partial" $Destination -Force
}
if (!$TrainedDataPath) {
    $TrainedDataPath = Join-Path $cache 'eng.traineddata'
    if (!(Test-Path $TrainedDataPath)) { Download 'https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main/eng.traineddata' $TrainedDataPath }
}
if ((Get-Item $TrainedDataPath).Length -lt 1000000) { throw 'Language model is missing or incomplete.' }
if (!$VCRedistPath) {
    $VCRedistPath = Join-Path $cache 'vc_redist.x64.exe'
    if (!(Test-Path $VCRedistPath)) { Download 'https://aka.ms/vs/17/release/vc_redist.x64.exe' $VCRedistPath }
}
$sig = Get-AuthenticodeSignature $VCRedistPath
if ($sig.Status -ne 'Valid' -or $sig.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') { throw 'The VC++ runtime must have a valid Microsoft signature.' }
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item (Join-Path $windows 'tessdata'), (Join-Path $windows 'x64'), (Join-Path $stage 'Licenses') -ItemType Directory -Force | Out-Null
$bin = Join-Path $root 'src\OCR2Geometry\bin\Release'
foreach ($dll in 'OCR2Geometry.dll','Tesseract.dll') { Copy-Item (Join-Path $bin $dll) $windows }
$native = @(Get-ChildItem (Join-Path $bin 'x64') -Filter '*.dll')
if (!($native.Name -match 'tesseract') -or !($native.Name -match 'lept')) { throw 'Tesseract x64 native libraries are missing from the build output.' }
$native | Copy-Item -Destination (Join-Path $windows 'x64')
Copy-Item $TrainedDataPath (Join-Path $windows 'tessdata\eng.traineddata')
Copy-Item (Join-Path $root 'installer\PackageContents.xml') $stage
Copy-Item (Join-Path $root 'installer\INSTALL.md') $stage
# Include upstream legal notices verbatim alongside the redistributed dependencies.
$notices = @{
    'Tesseract-wrapper-LICENSE.txt' = 'https://raw.githubusercontent.com/charlesw/tesseract/5.2.0/LICENSE.txt'
    'Tesseract-LICENSE.txt' = 'https://raw.githubusercontent.com/tesseract-ocr/tesseract/5.2.0/LICENSE'
    'Leptonica-license.txt' = 'https://raw.githubusercontent.com/DanBloomberg/leptonica/1.82.0/leptonica-license.txt'
    'tessdata-LICENSE.txt' = 'https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main/LICENSE'
}
foreach ($name in $notices.Keys) { Download $notices[$name] (Join-Path $stage "Licenses\$name") }
& (Join-Path $PSScriptRoot 'Test-Bundle.ps1') -BundlePath $stage
Get-ChildItem $stage -File -Recurse | ForEach-Object {
    '{0}  {1}' -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash, $_.FullName.Substring($stage.Length + 1)
} | Set-Content (Join-Path $out 'bundle-sha256.txt') -Encoding UTF8
$zip = Join-Path $out 'OCR2Geometry-0.9.0-bundle.zip'
Compress-Archive -Path $stage -DestinationPath $zip -Force
& $IsccPath "/DStageDir=$stage" "/DOutputDir=$out" "/DRedistPath=$VCRedistPath" (Join-Path $root 'installer\OCR2Geometry.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
$setup = Join-Path $out 'OCR2Geometry-0.9.0-Setup-x64.exe'
if (!(Test-Path $setup)) { throw 'Installer output is missing.' }
Get-FileHash $setup -Algorithm SHA256 | Format-List
Write-Host "Ready: $setup"
