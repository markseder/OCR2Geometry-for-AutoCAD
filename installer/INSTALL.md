# OCR2Geometry 0.9.0 — installation candidate

Supported target: **AutoCAD 2020 (full AutoCAD), Windows 10/11 x64**.
AutoCAD LT and other AutoCAD releases are not supported by this package.

## For the end user

1. Save drawings and close every AutoCAD process.
2. Run `OCR2Geometry-0.9.0-Setup-x64.exe` and approve the Windows administrator prompt.
3. Restart AutoCAD 2020 and run `OCR2GEOMETRY`.

Visual Studio, Python and NETLOAD are not required. The installer includes the
English Tesseract model, x64 OCR libraries and Microsoft's VC++ runtime installer.
.NET Framework 4.7.2 or later and AutoCAD 2020 must already be installed.
OCR itself works offline. No AutoCAD DLLs are included.

The bundle is installed for all users at:
`C:\Program Files\Autodesk\ApplicationPlugins\OCR2Geometry.bundle`.
The installer does not change AutoCAD security settings or drawing preferences.
An unsigned development installer may show Windows publisher/SmartScreen warnings;
verify its origin before running it. Production code signing is not configured.

Remove old manually installed OCR2Geometry bundles from other ApplicationPlugins
locations to avoid duplicate versions. Do not NETLOAD an older DLL in the same
AutoCAD session. Existing loose source/build files are not deleted by setup.

If the command is unavailable, check that AutoCAD's `APPAUTOLOAD` enables bundle
loading and that no older copy is already loaded. Keep SECURELOAD enabled. Restart
AutoCAD after installing. Administrator policies may restrict plugin loading.

Uninstall using Windows Settings > Apps > OCR2Geometry for AutoCAD. Close AutoCAD
first. Drawings, exported CSVs and the previous per-user OCR cache are preserved.
The shared Microsoft runtime is not removed.

## Build the installer (developer only)

On a Windows computer install:

- AutoCAD 2020 (provides the compile-time Autodesk assemblies).
- Visual Studio with .NET desktop development and .NET Framework 4.7.2 targeting pack.
- Inno Setup 6.3 or later.

Download/extract this branch, then run `Build-Installer.cmd`. Internet is needed
for NuGet restore, the language model, runtime and upstream license notices.
The script rebuilds Release, runs parser regressions, stages and validates the
bundle, then compiles the installer. A failure stops the build.

For custom installation paths, run from PowerShell at the repository root:

```powershell
.\scripts\Build-Installer.ps1 -Acad2020Dir 'D:\Autodesk\AutoCAD 2020' -IsccPath 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
```

Optional parameters: `-MSBuildPath`, `-TrainedDataPath`, `-VCRedistPath`.
The runtime executable must have a valid Microsoft Authenticode signature.
Cached downloads are kept under `artifacts/downloads`; remove that directory to
refresh them. Dependency downloads are not pinned to hashes; retain the generated
hash manifest and tested executable when distributing a release.

Outputs under `artifacts`:

- `OCR2Geometry-0.9.0-Setup-x64.exe` — installer to send to users.
- `OCR2Geometry-0.9.0-bundle.zip` — manual deployment for administrators; prerequisites are separate.
- `bundle-sha256.txt` — checksums of staged bundle files.

A GitHub **source ZIP is not an installer**. Build the EXE before distributing it.
The staged `Licenses` folder contains upstream dependency notices. Review upstream
redistribution terms for native dependencies and the Microsoft runtime before a
public release.

## Acceptance test before merging

- Build the EXE on Windows and install with AutoCAD closed.
- Open AutoCAD 2020, run OCR2GEOMETRY without NETLOAD; About shows 0.9.0.
- Disconnect internet and recognize a coordinate screenshot on a fresh user profile.
- Verify Points/Labels/Polyline layers, individual toggles, open/closed contour and row order.
- Run setup again to check upgrade/reinstallation, and verify setup refuses while AutoCAD is open.
- Uninstall with AutoCAD closed; after restart the command should no longer autoload.
- Verify drawings/CSV files and unrelated ApplicationPlugins are untouched.

This candidate still requires a Windows build and real AutoCAD installation test.
