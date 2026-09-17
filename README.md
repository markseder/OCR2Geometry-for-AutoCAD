# OCR2Geometry for AutoCAD

OCR2Geometry for AutoCAD is a lightweight AutoCAD plugin that converts coordinate tables from text, files, and images into drawing geometry.

The first target is **AutoCAD 2020**, using **C# / .NET Framework / WPF**.

## Current development build — v0.5.0

Already working and validated in AutoCAD 2020:

- `OCR2GEOMETRY` command
- editable X/Y/Z coordinate table
- add/delete rows
- X/Y swap
- configurable start point number
- configurable text height
- create `DBPoint` objects and numbered `DBText` labels at real Z elevation
- paste coordinates from clipboard
- import CSV/TXT
- export CSV as `Point,X,Y,Z`
- preserve explicit point numbers from imported data
- report skipped/unrecognized text rows
- select PNG/JPG/JPEG/BMP/TIF/TIFF images
- paste an image directly from the Windows clipboard
- preview the selected/pasted image inside the plugin
- local OCR using Tesseract 5 through a .NET package
- no Python, pip, virtual environment or external Python process
- OCR decimal-separator recovery with highlighted recovered cells

New in **v0.5.0**:

- coordinate table starts empty on every new OCR2Geometry window
- About / Donate window
- developer: **Pavel Matveev**
- email: **pavelmatveev84@gmail.com**
- project link: https://github.com/markseder/OCR2Geometry-for-AutoCAD
- USDT donation QR and wallet for **TRON (TRC20)**
- wallet address: `TLoqWbptHqejDnj14pSQDC211xyHp1ogUq`

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

## Build and test

1. Open `OCR2Geometry.sln` in Visual Studio.
2. Build the solution. Visual Studio should restore the `Tesseract` NuGet package automatically.
3. In AutoCAD 2020 run `NETLOAD` and load the built `OCR2Geometry.dll`.
4. Run `OCR2GEOMETRY`.
5. Press `Win+Shift+S` and capture only the coordinate table.
6. Click **Paste image** and verify the preview appears.
7. Click **Recognize OCR**.
8. On the first OCR run, allow the plugin to download the English Tesseract language model once.
9. Verify recognized coordinate rows populate the table.
10. Review any yellow OCR-recovered cells, correct if necessary, then use **Create points**.
11. Open **About / Donate** and verify developer information, GitHub link, QR and wallet copy action.

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

### v0.4 / v0.4.1 — Image OCR and validation
- [x] Image selection and preview
- [x] Paste image from clipboard
- [x] Local .NET Tesseract OCR
- [x] X/Y/Z support
- [x] OCR decimal-separator recovery
- [x] Highlight recovered cells
- [x] Validated in AutoCAD 2020

### v0.5.0 — About / Donate
- [x] Empty table on startup
- [x] Developer and contact information
- [x] GitHub project link
- [x] USDT TRC20 donation QR
- [x] Copy wallet address action

## Status

Active development. v0.4.1 is merged into `main`; v0.5.0 is under local validation before merge.
