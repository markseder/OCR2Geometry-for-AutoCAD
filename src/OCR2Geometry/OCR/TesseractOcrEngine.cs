using System;
using System.IO;
using System.Net;
using Tesseract;

namespace OCR2Geometry.OCR
{
    public sealed class TesseractOcrEngine : IOcrEngine
    {
        private const string Language = "eng";
        private const string TrainedDataUrl = "https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main/eng.traineddata";

        private readonly string _tessdataDirectory;

        public string Name => "Tesseract 5 (.NET, local)";

        public bool IsAvailable => true;

        public TesseractOcrEngine()
        {
            _tessdataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OCR2Geometry",
                "tessdata");
        }

        public OcrResult Recognize(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                throw new FileNotFoundException("OCR image was not found.", imagePath);
            }

            EnsureLanguageData();

            using (var engine = new TesseractEngine(_tessdataDirectory, Language, EngineMode.LstmOnly))
            {
                // Coordinate tables are mostly digits and separators. Restricting the alphabet
                // reduces common OCR substitutions while keeping decimal and sign characters.
                engine.SetVariable("tessedit_char_whitelist", "0123456789.,-+ ");
                engine.SetVariable("preserve_interword_spaces", "1");

                using (var image = Pix.LoadFromFile(imagePath))
                using (var page = engine.Process(image, PageSegMode.SingleBlock))
                {
                    return new OcrResult(page.GetText(), Name);
                }
            }
        }

        private void EnsureLanguageData()
        {
            Directory.CreateDirectory(_tessdataDirectory);
            var trainedDataPath = Path.Combine(_tessdataDirectory, Language + ".traineddata");

            if (File.Exists(trainedDataPath) && new FileInfo(trainedDataPath).Length > 1000000)
            {
                return;
            }

            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "OCR2Geometry-for-AutoCAD");
                    client.DownloadFile(TrainedDataUrl, trainedDataPath);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    if (File.Exists(trainedDataPath))
                    {
                        File.Delete(trainedDataPath);
                    }
                }
                catch
                {
                    // Ignore cleanup errors and report the original download error.
                }

                throw new InvalidOperationException(
                    "Tesseract language data could not be downloaded. " +
                    "Internet access is required once on the first OCR run. Details: " + ex.Message,
                    ex);
            }
        }
    }
}
