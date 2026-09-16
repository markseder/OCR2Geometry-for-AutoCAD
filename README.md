# OCR2Geometry for AutoCAD

OCR2Geometry for AutoCAD is a lightweight AutoCAD plugin that converts coordinate tables from text, files, and images into drawing geometry.

The first target is **AutoCAD 2020**, using **C# / .NET Framework / WPF**.

## Current development build — v0.4 local OCR

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
- paste an image directly from the Windows clipboard
- intended workflow: `Win+Shift+S` -> capture table -> **Paste image**
- preview the selected/pasted image inside the plugin
- local OCR using Tesseract 5 through a .NET package
- no Python, pip, virtual environment or external Python process
- recognized OCR text is passed into the existing coordinate parser
- first OCR run downloads `eng.traineddata` once into `%LOCALAPPDATA%\OCR2Geometry\tessdata`

## Build requirements

- Windows
- AutoCAD 2020 installed
- Visual Studio with .NET desktop development tools
- .NET Framework 4.7.2 targeting pack
- NuGet package restore enabled

The project expects the AutoCAD managed API assemblies in:

```text
C:\Program Files\Autodesk\AutoCAD 2020\acdbmgd.dll
C:\Program Files\Autodesk\AutoCAD 2020\acmgd.dll
C:\Program Files\Autodesk\AutoCAD 2020\accoremgd.dll
```

## Build and test v0.4

1. Open `OCR2Geometry.sln` in Visual Studio.
2. Build the solution. Visual Studio should restore the `Tesseract` NuGet package automatically.
3. In AutoCAD 2020 run `NETLOAD` and load the built `OCR2Geometry.dll`.
4. Run `OCR2GEOMETRY`.
5. Press `Win+Shift+S` and capture only the coordinate table.
6. Click **Paste image** and verify the preview appears.
7. Click **Recognize OCR**.
8. On the first OCR run, allow the plugin to download the English Tesseract language model once.
9. Verify recognized coordinate rows populate the table.
10. Correct any OCR mistakes manually, then use **Create points**.

You can still use **Select image** for saved screenshots/photos and all v0.3 CSV/TXT/clipboard-text workflows remain available.

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
- [x] Paste image from clipboard
- [x] OCR engine abstraction
- [x] Local .NET Tesseract OCR adapter
- [x] Feed OCR text into the coordinate parser
- [ ] Validate OCR quality on real coordinate-table screenshots
- [ ] Recognition confidence / validation workflow

### Later improvements

- [ ] Bundle/fallback language data for fully offline first launch
- [ ] OCR preprocessing for low-contrast/scanned tables
- [ ] User-defined text offset
- [ ] Improved AutoCAD layer/settings workflow
- [ ] Support additional AutoCAD versions

## Status

Active development. v0.3 is merged into `main`; v0.4 local OCR is ready for build and real-table testing before merge.
