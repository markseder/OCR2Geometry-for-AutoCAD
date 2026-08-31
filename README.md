# OCR2Geometry for AutoCAD

OCR2Geometry for AutoCAD is a lightweight AutoCAD plugin that converts coordinate tables from images into drawing geometry.

The project is being developed first for **AutoCAD 2020** using **C# / .NET Framework**.

## Planned MVP

- Recognize planar coordinate tables from images
- Work with X/Y coordinates only
- Swap X and Y columns
- Review and edit recognized values before import
- Create AutoCAD POINT objects
- Add point numbers next to created points
- Export recognized coordinates to CSV

## First development target

`NETLOAD` the plugin DLL in AutoCAD 2020, run the command:

```text
OCR2GEOMETRY
```

and open the plugin window. The first development build will also be able to create several test points in the current drawing.

## Technology

- C#
- .NET Framework 4.7.2
- AutoCAD 2020 .NET API
- WPF user interface

## Project status

Early development / bootstrap stage.
