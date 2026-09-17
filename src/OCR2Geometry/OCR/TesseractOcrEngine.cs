using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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
            var preparedPath = PrepareForOcr(imagePath);

            try
            {
                using (var engine = new TesseractEngine(_tessdataDirectory, Language, EngineMode.LstmOnly))
                {
                    engine.SetVariable("tessedit_char_whitelist", "0123456789.,-+ ");
                    engine.SetVariable("preserve_interword_spaces", "1");
                    engine.SetVariable("classify_bln_numeric_mode", "1");
                    engine.SetVariable("user_defined_dpi", "300");

                    var modes = new[]
                    {
                        PageSegMode.SparseText,
                        PageSegMode.Auto,
                        PageSegMode.SingleBlock
                    };

                    var bestText = string.Empty;
                    var bestScore = -1;

                    foreach (var mode in modes)
                    {
                        using (var image = Pix.LoadFromFile(preparedPath))
                        using (var page = engine.Process(image, mode))
                        {
                            var text = page.GetText() ?? string.Empty;
                            var score = ScoreText(text);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestText = text;
                            }
                        }
                    }

                    if (string.IsNullOrWhiteSpace(bestText))
                    {
                        using (var image = Pix.LoadFromFile(imagePath))
                        using (var page = engine.Process(image, PageSegMode.SparseText))
                        {
                            bestText = page.GetText() ?? string.Empty;
                        }
                    }

                    return new OcrResult(bestText, Name);
                }
            }
            finally
            {
                try
                {
                    if (!string.Equals(preparedPath, imagePath, StringComparison.OrdinalIgnoreCase) && File.Exists(preparedPath))
                    {
                        File.Delete(preparedPath);
                    }
                }
                catch
                {
                }
            }
        }

        private static int ScoreText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            var digitCount = text.Count(char.IsDigit);
            var separatorCount = text.Count(c => c == ',' || c == '.' || c == '-' || c == '+');
            var lineCount = text.Count(c => c == '\n');
            return digitCount * 10 + separatorCount * 3 + lineCount;
        }

        private static string PrepareForOcr(string sourcePath)
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), "OCR2Geometry", "ocr");
            Directory.CreateDirectory(tempDirectory);
            var targetPath = Path.Combine(tempDirectory, "ocr_" + Guid.NewGuid().ToString("N") + ".png");

            using (var source = new Bitmap(sourcePath))
            {
                var scale = 3;
                var width = Math.Min(source.Width * scale, 6000);
                var height = Math.Min(source.Height * scale, 6000);

                using (var prepared = new Bitmap(width, height, PixelFormat.Format24bppRgb))
                using (var graphics = Graphics.FromImage(prepared))
                using (var attributes = new ImageAttributes())
                {
                    graphics.Clear(Color.White);
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;

                    var grayMatrix = new ColorMatrix(new[]
                    {
                        new[] { 0.299f, 0.299f, 0.299f, 0f, 0f },
                        new[] { 0.587f, 0.587f, 0.587f, 0f, 0f },
                        new[] { 0.114f, 0.114f, 0.114f, 0f, 0f },
                        new[] { 0f, 0f, 0f, 1f, 0f },
                        new[] { 0f, 0f, 0f, 0f, 1f }
                    });

                    attributes.SetColorMatrix(grayMatrix);
                    attributes.SetThreshold(0.72f);

                    graphics.DrawImage(
                        source,
                        new Rectangle(0, 0, width, height),
                        0,
                        0,
                        source.Width,
                        source.Height,
                        GraphicsUnit.Pixel,
                        attributes);

                    prepared.Save(targetPath, System.Drawing.Imaging.ImageFormat.Png);
                }
            }

            return targetPath;
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
                }

                throw new InvalidOperationException(
                    "Tesseract language data could not be downloaded. " +
                    "Internet access is required once on the first OCR run. Details: " + ex.Message,
                    ex);
            }
        }
    }
}
