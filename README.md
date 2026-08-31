# OCR2Geometry for AutoCAD

OCR2Geometry for AutoCAD is a lightweight AutoCAD plugin that converts coordinate tables from images into drawing geometry.

The first target is **AutoCAD 2020**, using **C# / .NET Framework / WPF**.

## MVP scope

- Recognize planar coordinate tables from images
- Work with X/Y coordinates only
- Swap X and Y columns
- Review and edit recognized values before import
- Create AutoCAD `DBPoint` objects
- Add point-number `DBText` labels next to points
- Export coordinates to CSV

No latitude/longitude or coordinate-system transformation is planned for the first MVP.

## Current development build — v0.2

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
2. Build the solution in `Debug` or `Release` mode.
3. Start AutoCAD 2020.
4. Run `NETLOAD`.
5. Select the built `OCR2Geometry.dll`, normally from:

```text
src\OCR2Geometry\bin\Debug\OCR2Geometry.dll
```

6. Run `OCR2GEOMETRY`.
7. Edit/add/delete coordinate rows as required.
8. Test `Swap X/Y`.
9. Set a start number and text height.
10. Click **Create points** and verify points/labels in Model Space.
11. Click **Export CSV** and verify the saved coordinate table.

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
- [ ] Validate v0.2 build in AutoCAD 2020

### v0.3 — OCR foundation

- [ ] Image selection
- [ ] OCR engine integration
- [ ] Coordinate table parser
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
│       ├── Models/
│       ├── Properties/
│       ├── UI/
│       └── OCR2Geometry.csproj
└── README.md
```

## Status

Active development. v0.1 has been validated in AutoCAD 2020; v0.2 is ready for local build/testing before merge to `main`.
