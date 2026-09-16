namespace OCR2Geometry.OCR
{
    public sealed class OcrResult
    {
        public string Text { get; }
        public string EngineName { get; }

        public OcrResult(string text, string engineName)
        {
            Text = text ?? string.Empty;
            EngineName = engineName ?? string.Empty;
        }
    }
}
