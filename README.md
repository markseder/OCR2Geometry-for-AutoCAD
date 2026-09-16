# OCR2Geometry for AutoCAD

OCR2Geometry for AutoCAD is a lightweight AutoCAD plugin that converts coordinate tables from text, files, and images into drawing geometry.

The first target is **AutoCAD 2020**, using **C# / .NET Framework / WPF**.

## Current development build — v0.4 foundation

Already working and validated in AutoCAD 2020:

- `OCR2GEOMETRY` command
- editable X/Y coordinate table
- add/delete rows
- X/Y swap
- configurable start point number
- configurable text height
- create `DBPoint` objects and numbered `DBText` labels
- paste coordinates from clipboard
- import CSV/TXT
- export CSV
- preserve explicit point numbers from imported data
- report skipped/unrecognized text rows

New in the v0.4 development branch:

- select PNG/JPG/JPEG/BMP/TIF/TIFF image
- preview the selected image inside the plugin
- OCR engine abstraction (`IOcrEngine`)
- OCR result model ready to feed recognized text into the existing coordinate parser

The actual local OCR engine adapter is the next step inside v0.4. The AutoCAD geometry and coordinate parser no longer need to know which OCR engine is used.

## Build requirements

- Windows
- AutoCAD 2020 installed
- Visual Studio with .NET desktop development tools
- .NET Framework 4.7.2 targeting pack

The project expects the AutoCAD managed API assemblies in:

```text
C:\Program Files\Autodesk\AutoCAD 2020\acdbmgd.dll
C:\Program Files\Autodesk\AutoCAD 2020\acmgd.dll
C:\Program Files\Autodesk\AutoCAD 2020\accoremgd.dll
```

## Build and test

1. Open `OCR2Geometry.sln` in Visual Studio.
2. Build the solution.
3. In AutoCAD 2020 run `NETLOAD` and load the built `OCR2Geometry.dll`.
4. Run `OCR2GEOMETRY`.
5. Click **Select image** and choose a coordinate-table screenshot.
6. Verify the preview appears in the right-hand panel.
7. Verify existing clipboard/CSV import and point creation still work.

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

### v0.3 — Import and validation

- [x] Clipboard text import
- [x] CSV/TXT import
- [x] Coordinate text parser
- [x] Invalid-line reporting
- [x] Preserve explicit imported point numbers
- [x] Validated in AutoCAD 2020

### v0.4 — Image OCR

- [x] Image selection
- [x] Image preview
- [x] OCR engine abstraction
- [ ] Local OCR engine adapter
- [ ] Feed OCR text into the coordinate parser
- [ ] Recognition confidence / validation workflow

### Later improvements

- [ ] User-defined text offset
- [ ] Improved AutoCAD layer/settings workflow
- [ ] Support additional AutoCAD versions

## Status

Active development. v0.3 is merged into `main`; v0.4 image/OCR work is in progress.
