using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using OCR2Geometry.Import;
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

        public OcrResult Recognize(string imagePath, int expectedColumns = 4, bool numbered = true)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                throw new FileNotFoundException("OCR image was not found.", imagePath);
            }

            EnsureLanguageData();
            var preparedPaths = new List<string>();

            try
            {
                preparedPaths.Add(PrepareForOcr(imagePath, true));
                preparedPaths.Add(PrepareForOcr(imagePath, false));
                using (var engine = new TesseractEngine(_tessdataDirectory, Language, EngineMode.LstmOnly))
                {
                    engine.SetVariable("tessedit_char_whitelist", "0123456789.,-+ ");
                    engine.SetVariable("preserve_interword_spaces", "1");
                    engine.SetVariable("classify_bln_numeric_mode", "1");
                    engine.SetVariable("user_defined_dpi", "300");

                    var modes = new[]
                    {
                        PageSegMode.SingleBlock,
                        PageSegMode.Auto,
                        PageSegMode.SparseText
                    };

                    var bestText = string.Empty;
                    var bestScore = int.MinValue;

                    foreach (var preparedPath in preparedPaths.Concat(new[] { imagePath }))
                    foreach (var mode in modes)
                    {
                        using (var image = Pix.LoadFromFile(preparedPath))
                        using (var page = engine.Process(image, mode))
                        {
                            var text = page.GetText() ?? string.Empty;
                            var score = ScoreText(text, expectedColumns, numbered);
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
                    foreach (var preparedPath in preparedPaths)
                    {
                        if (File.Exists(preparedPath)) File.Delete(preparedPath);
                    }
                }
                catch
                {
                }
            }
        }

        private static int ScoreText(string text, int expectedColumns, bool numbered)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            // Prefer complete, consistently structured coordinate rows, not the most digits.
            var parsed = TextCoordinateParser.ParseOcr(text, 1, expectedColumns, numbered);
            return parsed.Points.Count * 1000 - parsed.InvalidLineNumbers.Count * 100
                - parsed.RecoveredDecimalCount * 10;
        }

        private static string PrepareForOcr(string sourcePath, bool removeGrid)
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), "OCR2Geometry", "ocr");
            Directory.CreateDirectory(tempDirectory);
            var targetPath = Path.Combine(tempDirectory, "ocr_" + Guid.NewGuid().ToString("N") + ".png");

            using (var source = new Bitmap(sourcePath))
            {
                var scale = Math.Min(3.0, 6000.0 / Math.Max(source.Width, source.Height));
                var width = Math.Max(1, (int)Math.Round(source.Width * scale));
                var height = Math.Max(1, (int)Math.Round(source.Height * scale));

                using (var prepared = new Bitmap(width, height, PixelFormat.Format24bppRgb))
                {
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

                    }
                    if (removeGrid) RemoveGridLines(prepared);
                    prepared.Save(targetPath, System.Drawing.Imaging.ImageFormat.Png);
                }
            }

            return targetPath;
        }

        private static void RemoveGridLines(Bitmap bitmap)
        {
            var width = bitmap.Width;
            var height = bitmap.Height;
            var data = bitmap.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            try
            {
                var pixels = new byte[data.Stride * height];
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
                var erase = new bool[width * height];
                // Detect long uninterrupted strokes; mark both directions before erasing.
                for (var y = 0; y < height; y++)
                {
                    var start = -1;
                    for (var x = 0; x <= width; x++)
                    {
                        if (x < width && pixels[y * data.Stride + x * 3] < 128)
                        {
                            if (start < 0) start = x;
                        }
                        else if (start >= 0)
                        {
                            if (x - start >= Math.Max(40, width / 3))
                                for (var k = start; k < x; k++) erase[y * width + k] = true;
                            start = -1;
                        }
                    }
                }
                for (var x = 0; x < width; x++)
                {
                    var start = -1;
                    for (var y = 0; y <= height; y++)
                    {
                        if (y < height && pixels[y * data.Stride + x * 3] < 128)
                        {
                            if (start < 0) start = y;
                        }
                        else if (start >= 0)
                        {
                            if (y - start >= Math.Max(40, height / 3))
                                for (var k = start; k < y; k++) erase[k * width + x] = true;
                            start = -1;
                        }
                    }
                }
                for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    if (!erase[y * width + x]) continue;
                    var offset = y * data.Stride + x * 3;
                    pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = 255;
                }
                Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            }
            finally
            {
                bitmap.UnlockBits(data);
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
                }

                throw new InvalidOperationException(
                    "Tesseract language data could not be downloaded. " +
                    "Internet access is required once on the first OCR run. Details: " + ex.Message,
                    ex);
            }
        }
    }
}
