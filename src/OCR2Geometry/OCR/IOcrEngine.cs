namespace OCR2Geometry.OCR
{
    public enum OcrMode { Auto, TableCells, Text }

    public interface IOcrEngine
    {
        string Name { get; }
        bool IsAvailable { get; }
        OcrResult Recognize(string imagePath, int expectedColumns = 4, bool numbered = true, OcrMode mode = OcrMode.Auto);
    }
}
