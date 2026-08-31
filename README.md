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
- Export recognized coordinates to CSV

No latitude/longitude or coordinate-system transformation is planned for the first MVP.

## Current development build

The bootstrap build already contains:

- AutoCAD plugin entry point
- `OCR2GEOMETRY` command
- WPF plugin window
- test coordinate model
- creation of `DBPoint` objects in Model Space
- creation of point-number `DBText` labels

Test coordinates currently used by the bootstrap build:

```text
1    512345.23    6876543.11
2    512351.86    6876551.42
3    512360.14    6876567.30
```

## Build requirements

- Windows
- AutoCAD 2020 installed
- Visual Studio 2022 with .NET desktop development tools
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
4. Run:

```text
NETLOAD
```

5. Select the built `OCR2Geometry.dll`, normally from:

```text
src\OCR2Geometry\bin\Debug\OCR2Geometry.dll
```

6. Run:

```text
OCR2GEOMETRY
```

7. Click **Create test points**.

The bootstrap build should create three points and their point numbers in Model Space.

## Planned development stages

### v0.1 — AutoCAD bootstrap

- [x] Plugin project
- [x] `OCR2GEOMETRY` command
- [x] WPF window
- [x] Test point creation
- [x] Point numbering labels

### v0.2 — Coordinate table

- [ ] Editable coordinate grid
- [ ] Add/delete rows
- [ ] Start-number setting
- [ ] Text height and text offset settings
- [ ] X/Y swap

### v0.3 — CSV

- [ ] Export recognized/edited table to CSV

### v0.4 — OCR

- [ ] Image selection
- [ ] OCR engine integration
- [ ] Coordinate table parser
- [ ] Recognition confidence / validation workflow

## Project structure

```text
OCR2Geometry-for-AutoCAD/
├── OCR2Geometry.sln
├── src/
│   └── OCR2Geometry/
│       ├── AutoCAD/
│       ├── Commands/
│       ├── Models/
│       ├── Properties/
│       ├── UI/
│       └── OCR2Geometry.csproj
└── README.md
```

## Status

Early development. The current branch is intended to prove the AutoCAD 2020 plugin loading and geometry-writing workflow before OCR is added.
