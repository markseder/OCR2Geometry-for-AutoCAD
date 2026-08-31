using Autodesk.AutoCAD.Runtime;
using OCR2Geometry.UI;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace OCR2Geometry.Commands
{
    public class OcrCommand
    {
        [CommandMethod("OCR2GEOMETRY", CommandFlags.Modal)]
        public void OpenMainWindow()
        {
            var window = new MainWindow();
            AcApp.ShowModalWindow(window);
        }
    }
}
