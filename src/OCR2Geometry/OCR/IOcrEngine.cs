namespace OCR2Geometry.OCR
{
    public interface IOcrEngine
    {
        string Name { get; }
        bool IsAvailable { get; }
        OcrResult Recognize(string imagePath);
    }
}
