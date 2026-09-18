# OCR2Geometry for AutoCAD

OCR2Geometry for AutoCAD is a lightweight AutoCAD plugin that converts coordinate tables from text, files, and images into drawing geometry.

The first target is **AutoCAD 2020**, using **C# / .NET Framework / WPF**.

## Current development build — v0.9.0 candidate

The tested v0.8.0 is merged into `main`. Version 0.9.0 adds a Windows installer and command autoload bundle; installation testing is pending.

See [installation and installer build instructions](installer/INSTALL.md). Developers can run `Build-Installer.cmd` on Windows to produce the distributable EXE.

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

Active development. v0.8.0 is validated by the user and merged into `main`; v0.9.0 installer validation is pending.

The sections below are historical development notes; their pending-test statements describe the time those versions were developed.


## v0.6.0 OCR candidate

v0.5.0 was approved and merged into main. This branch is NOT validated in AutoCAD yet.

- Choose the source image column order: Point X Y Z (default), Point X Y, X Y Z, or X Y. This affects OCR only.
- OCR rejects rows with missing/extra numeric fields instead of silently shifting columns.
- Commas in OCR are decimal separators, never CSV delimiters.
- Try both grid-cleaned and unchanged preprocessing, plus the original image. Rank results using complete rows in the selected layout instead of digit count.
- Preserve aspect ratio when limiting image size.
- Decimal recovery requires at least two matching samples and a strict majority; recovered coordinates stay yellow.

### Verification

In a Visual Studio Developer Command Prompt:

```bat
msbuild tests\ParserRegression\ParserRegression.csproj /p:Configuration=Release
tests\ParserRegression\bin\ParserRegression.exe
```

These tests compile the actual parser and model without AutoCAD. They cover the screenshot reference, shifted/missing fields, four layouts, decimal recovery, and CSV/text regression. They were added but could not be executed in the Linux editing environment (no C# compiler). Full plugin compilation and AutoCAD 2020 testing remain required.

A separate Tesseract 5.3.4 CLI experiment on the reduced preview from the screenshot confirmed better row extraction after removing grid lines, but still misread individual digits. This is not an end-to-end plugin test. Use the original table image for acceptance and compare every coordinate; clean row structure is not proof of numeric accuracy. Nine OCR passes may take longer than v0.5.0.

Reference values from the screenshot:

| Point | X | Y | Z |
|---|---|---|---|
| 1 | 72968.58 | 65990.98 | 725.50 |
| 2 | 70174.18 | 69426.41 | 786.00 |
| 3 | 68041.52 | 71178.59 | 865.70 |
| 4 | 66510.06 | 69358.06 | 800.00 |
| 5 | 70850.81 | 63803.75 | 445.00 |

Do not merge v0.6.0 until the user tests it in AutoCAD 2020 and explicitly approves.

### v0.6.0 revision 2 — OCR modes and diagnostics

- Auto (default): uses individual cells when a complete straight grid matches the selected layout, otherwise uses Text. It does not silently replace an unreadable cell result with a guessed number.
- Table cells: detects the full grid, crops each cell to its ink with white padding, then tries SingleLine and SingleWord. Both valid readings must agree numerically. Missing/conflicting cells remain marked in diagnostics, and affected rows are skipped without shifting columns or inventing point numbers.
- Text: previous multi-pass recognition for images without a complete grid.
- OCR details shows raw readings, chosen text, and specific skip reasons, even when no rows import. Copy text from this window when reporting an issue.
- Capture only the source table including all outer borders and a small white margin. Skewed, broken, merged-cell or partly cropped grids are not supported by Table cells; use Text.
- DLL file version: 0.6.0.2; assembly version stays 0.6.0.0.

Validation: a separate Tesseract CLI experiment on the latest screenshot's table crop found 6 horizontal/5 vertical boundaries. Tight cell crops with padding and SingleLine/SingleWord yielded all 20 expected values, including Point 1 and negative Y values (invalid alternatives rejected). This experiment uses a different preprocessing library and does not prove .NET/WPF runtime behavior. C# compilation, regression harness execution, and AutoCAD 2020 tests remain unavailable in this environment.

Acceptance: rebuild, restart AutoCAD to unload the old DLL, NETLOAD, verify revision 2 label, choose Point X Y Z and Table cells, recognize the original five-row table. Expect Point 1 / X 252956.80 / Y -128368.72 / Z 725.50 and compare all other rows. Check Auto and Text, a missing cell, no detected grid, all four layouts, and OCR details. Keep PR unmerged pending approval.

### v0.6.0 revision 3 — fix regression confirmed by AutoCAD log

Revision 2 failed the user's real AutoCAD test: SingleWord removed punctuation and vetoed correct SingleLine coordinates. The earlier CLI screenshot experiment did not reproduce the Windows result and must not be treated as acceptance.

- Coordinates now use SingleLine and SingleBlock, trying thresholded and grayscale crops. The first valid reading has priority; secondary readings are logged, not allowed to veto it. SingleWord is excluded for coordinates.
- Normalize spaces on either side of an existing decimal mark (including `246443 ,23`); never concatenate separated digit groups or repair doubled minus signs.
- Point numbers use SingleBlock, SingleChar and SingleWord on both crops. At least two agreeing reads and a unique winning value are required. No point number is inferred from row order.
- Auto compares cell and text candidates even when a grid exists, ranking by complete parsed rows. Table cells remains available for explicit cell-only diagnosis.
- All OCR-imported rows are highlighted for source verification, including point numbers; alternate readings remain visible in OCR details. Existing decimal-recovery cell highlighting remains.
- UI: revision 3; DLL file version 0.6.0.3.

Regression cases cover the supplied log's punctuation, duplicate signs, coordinate-selection priority, and point agreement rules. The C# test harness and full AutoCAD plugin could not be built/run here (no C# compiler or AutoCAD). Static markup/handler/project checks and git diff --check are the available validation; numeric accuracy still needs the user's Windows test. Auto can be slower because it also evaluates text candidates. First-valid selection and repeated-number agreement do not guarantee OCR accuracy.

Test both Auto and Table cells on the ORIGINAL table image, including all borders. Verify all five rows and signs, especially Point 1, Point 3 and X=246443.23 for Point 4. If a number remains unreadable, send OCR details plus the original cropped input PNG (the screenshot preview is rescaled and is not the same OCR input).

### v0.6.0 revision 4 — keep coordinates when Point is unreadable

- Point is now a nullable, editable integer. Missing numbers display as empty, salmon-highlighted cells with an explanatory tooltip.
- Cell OCR retains a row with an unreadable Point when X/Y(/Z) are valid. Text OCR also retains a row missing its leading Point when the remaining field count matches the selected layout and the first token is not an integer. Ambiguous integer-leading short text rows are still skipped instead of shifting coordinates.
- Auto selection counts retained coordinate rows and prefers fewer missing numbers on otherwise equal results. Diagnostics distinguish retained rows awaiting Point entry from skipped coordinate rows.
- Renumber from Start explicitly replaces ALL numbers in the current displayed order, starting at Start number. Range overflow is checked before changing any number. Manual Point edits update the missing-number highlight.
- Create points and CSV export require filled Point numbers and committed valid edits; geometry creation also checks missing numbers before opening a transaction.
- The actual user log is added as a regression case: expect five rows, first Point empty, first XYZ 252956.80 / -128368.72 / 725.50, and subsequent Points 2/3/4/5 unchanged. Other cases cover empty tab cells, Point XY, and ambiguous integer-leading rows.

UI revision 4 / DLL file version 0.6.0.4. C# regression execution and the full Windows/AutoCAD build remain unverified because no compiler or AutoCAD is available. Perform the actual log/image test, enter Point 1 manually, clear it and check Create points is blocked, then test Renumber from Start and CSV export. Keep PR unmerged pending user validation.


## v0.7.0 — English tooltips and optional closed contour

All plugin UI tooltips are English, including the recovered-decimal warning. Input parsing still recognizes Russian table headers.

Enable **Create closed polyline** before clicking **Create points** to add a closed contour alongside the points and labels. The checkbox defaults to off. Vertices follow the current displayed row order (including sorting); the last vertex connects to the first through the entity's Closed property. With equal Z, a lightweight 2D polyline is created at that elevation. With varying Z, a simple 3D polyline preserves all elevations. No flattening is applied.

The contour requires at least three distinct positions. Exact consecutive duplicate positions and an explicitly repeated closing position are omitted from contour vertices only; input rows, point entities and labels are preserved. Coordinates must be finite. Validation runs before any geometry is written, and the points, labels and contour share one transaction. The contour follows the supplied order and does not repair crossings or calculate a convex hull.

Validation available here: static source/markup/handler checks and git diff --check. Full .NET Framework/WPF/AutoCAD compilation and runtime tests are not available in the editing environment.

AutoCAD 2020 acceptance: rebuild and restart before NETLOAD; verify version 0.7.0; check the recovered-decimal tooltip; with checkbox off verify only points/labels; with checkbox on verify a square at Z=0, a square at common nonzero Z and a contour with varying Z; verify Closed=Yes and last-to-first edge; sort table and verify displayed order; test repeated closing point and fewer than three distinct positions (no partial creation). Confirm blank Point still blocks creation and OCR/CSV/About remain functional. Do not merge before user approval.

## v0.8.0 — output layers, independent objects and row order

- Independent Points, Labels and Polyline checkboxes. Points and Labels default on, Polyline off. Create objects generates only selected types.
- Exact output layers: Points for DBPoint, Labels for DBText, Polyline for either lightweight or 3D polylines. Missing layers are created only for selected types. Existing layer settings and current drawing layer are preserved. A locked target layer raises a clear error; the transaction rolls back without partial objects/layers. Existing off/frozen layers remain off/frozen.
- Closed toggles last-to-first closure. Open polylines need 2 distinct positions; closed ones need 3. Equal Z produces a lightweight polyline at that elevation, varying Z a 3D polyline. In open mode an explicitly repeated last vertex is retained.
- Move up / Move down supports multiple selected rows and preserves their relative order. It first captures displayed order, clears sort descriptors and moves the selection one row where possible. Point numbers and coordinates do not change. The contour follows the resulting displayed order.
- Missing Point numbers block Labels, but do not prevent Points-only or Polyline-only creation. Text-height validation applies only when Labels is checked. CSV still requires Point numbers.

Validation: static markup/handler/name checks and git diff --check. No C# compiler, Windows WPF or AutoCAD runtime is available, so compilation and interactive behavior remain unverified.

AutoCAD 2020 acceptance: rebuild/restart/NETLOAD; verify v0.8.0; test each output alone and all together, checking exact entity layers with Properties; use a nonzero current layer; repeat using existing layers and a locked layer (no partial output). Test open two-point and closed three-point paths, equal/varying Z, repeated end vertex, invalid minimum sizes, all checkboxes off, and missing Point with Labels on/off. Move one row and contiguous/disjoint selections both ways, including top/bottom and a sorted grid; verify numbers stay unchanged and contour follows visible order. Check default window and minimum width, OCR/import/export/About. Keep unmerged until user approval.

## v0.9.0 — installer and AutoCAD command autoload

- Inno Setup EXE source and `Build-Installer.cmd` build entry point.
- AutoCAD 2020 x64 bundle installed under Program Files; run `OCR2GEOMETRY` without NETLOAD.
- Bundled English OCR model and native libraries; shared Microsoft VC++ runtime prerequisite included in setup.
- Setup/uninstall requires AutoCAD to be closed and preserves drawings and user data.
- Bundle validation rejects missing required files, wrong assembly versions and Autodesk reference DLLs.

Validation performed: PowerShell scripts parsed successfully; 26 existing parser regression checks passed using the actual C# sources on .NET 8 in Linux. Bundle validator exercised with a synthetic assembly/file fixture, including rejection of Autodesk DLLs and a missing model. This does **not** validate a real plugin bundle. Full .NET Framework/WPF build, Inno Setup compilation, runtime prerequisite installation, command autoload and uninstall still require Windows/AutoCAD 2020 testing. No built EXE is included in this PR.
