using System.Reflection;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Runtime;
using OCR2Geometry.Commands;

[assembly: AssemblyTitle("OCR2Geometry for AutoCAD")]
[assembly: AssemblyDescription("OCR coordinate table to AutoCAD geometry plugin")]
[assembly: AssemblyCompany("OCR2Geometry")]
[assembly: AssemblyProduct("OCR2Geometry for AutoCAD")]
[assembly: AssemblyVersion("0.1.0.0")]
[assembly: AssemblyFileVersion("0.1.0.0")]
[assembly: ComVisible(false)]
[assembly: Guid("9c678cf3-6cf5-4610-b405-762236d77a29")]
[assembly: ExtensionApplication(typeof(OCR2Geometry.Plugin))]
[assembly: CommandClass(typeof(OcrCommand))]
