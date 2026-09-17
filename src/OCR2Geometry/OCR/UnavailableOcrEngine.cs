using System;

namespace OCR2Geometry.OCR
{
    public sealed class UnavailableOcrEngine : IOcrEngine
    {
        public string Name => "OCR engine not connected";
        public bool IsAvailable => false;

        public OcrResult Recognize(string imagePath, int expectedColumns = 4, bool numbered = true, OcrMode mode = OcrMode.Auto)
        {
            throw new InvalidOperationException(
                "The image workflow is ready, but an OCR engine has not been connected yet. " +
                "The next step is to add a local OCR adapter without changing the AutoCAD/table workflow.");
        }
    }
}
