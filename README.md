# OCR2Geometry for AutoCAD

OCR2Geometry for AutoCAD is a lightweight AutoCAD plugin that converts coordinate tables from text, files, and eventually images into drawing geometry.

The first target is **AutoCAD 2020**, using **C# / .NET Framework / WPF**.

## MVP scope

- Work with planar X/Y coordinates
- Review and edit values before import
- Swap X and Y columns
- Create AutoCAD `DBPoint` objects
- Add point-number `DBText` labels next to points
- Import coordinates from clipboard / CSV / TXT
- Export coordinates to CSV
- OCR from images in a later stage

No latitude/longitude or coordinate-system transformation is planned for the first MVP.

## Current development build — v0.3

The current development branch contains:

- `OCR2GEOMETRY` command
- editable X/Y coordinate table
- add and delete table rows
- X/Y swap
- configurable start point number
- configurable text height
- creation of `DBPoint` objects in Model Space
- creation of point-number `DBText` labels
- CSV export with `Point,X,Y` columns
- paste coordinate text from clipboard
- import coordinate rows from CSV/TXT
- parsing of tab, semicolon, whitespace and plugin CSV formats
- skipped-line reporting for unrecognized text rows

Three sample coordinates are preloaded for quick testing:

```text
1    512345.23    6876543.11
2    512351.86    6876551.42
3    512360.14    6876567.30
```

## Build requirements

- Windows
- AutoCAD 2020 installed
- Visual Studio with .NET desktop development tools
- .NET Framework 4.7.2 targeting pack

The project expects the AutoCAD managed API assemblies in the default installation directory:

```text
C:\Program Files\Autodesk\AutoCAD 2020\acdbmgd.dll
C:\Program Files\Autodesk\AutoCAD 2020\acmgd.dll
C:\Program Files\Autodesk\AutoCAD 2020\accoremgd.dll
```

If AutoCAD is installed elsewhere, set the MSBuild property `Acad2020Dir` to the correct folder.

AutoCAD references use `Copy Local = False` so Autodesk DLLs are not copied into the plugin output or repository.

## Build and test

1. Open `OCR2Geometry.sln` in Visual Studio.
2. Build the solution.
3. Start AutoCAD 2020.
4. Run `NETLOAD` and load `OCR2Geometry.dll`.
5. Run `OCR2GEOMETRY`.
6. Test **Paste coordinates** using copied rows such as:

```text
1 512345.23 6876543.11
2 512351.86 6876551.42
3 512360.14 6876567.30
```

7. Test **Import CSV/TXT** using a CSV exported by the plugin.
8. Verify add/delete, `Swap X/Y`, start number and text height.
9. Click **Create points** and verify geometry/labels in Model Space.
10. Export CSV and verify the resulting file.

## Development stages

### v0.1 — AutoCAD bootstrap

- [x] Plugin project
- [x] `OCR2GEOMETRY` command
- [x] WPF window
- [x] Test point creation
- [x] Point numbering labels
- [x] Validated in AutoCAD 2020

### v0.2 — Coordinate workflow

- [x] Editable coordinate grid
- [x] Add/delete rows
- [x] Start-number setting
- [x] Text-height setting
- [x] X/Y swap
- [x] Create points from the edited table
- [x] CSV export
- [x] Validated in AutoCAD 2020

### v0.3 — Import and validation foundation

- [x] Clipboard text import
- [x] CSV/TXT import
- [x] Coordinate text parser
- [x] Invalid-line reporting
- [ ] Validate v0.3 build in AutoCAD 2020

### v0.4 — OCR

- [ ] Image selection
- [ ] Image preview
- [ ] OCR engine integration
- [ ] Feed recognized text into the existing coordinate parser
- [ ] Recognition confidence / validation workflow

### Later improvements

- [ ] User-defined text offset
- [ ] Improved AutoCAD layer/settings workflow
- [ ] Support additional AutoCAD versions

## Project structure

```text
OCR2Geometry-for-AutoCAD/
├── OCR2Geometry.sln
├── src/
│   └── OCR2Geometry/
│       ├── AutoCAD/
│       ├── Commands/
│       ├── Export/
│       ├── Import/
│       ├── Models/
│       ├── Properties/
│       ├── UI/
│       └── OCR2Geometry.csproj
└── README.md
```

## Status

Active development. v0.1 and v0.2 have been validated in AutoCAD 2020. v0.3 is ready for local build/testing before merge to `main`.
