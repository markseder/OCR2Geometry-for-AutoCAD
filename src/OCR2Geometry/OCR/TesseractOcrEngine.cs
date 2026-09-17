using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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

        public OcrResult Recognize(string imagePath, int expectedColumns = 4, bool numbered = true, OcrMode mode = OcrMode.Auto)
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

                    var diagnostics = new StringBuilder();
                    diagnostics.AppendLine("Requested mode: " + mode);
                    if (mode != OcrMode.Text)
                    {
                        var cells = RecognizeCells(engine, preparedPaths[1], expectedColumns, numbered, diagnostics);
                        if (cells != null) return new OcrResult(cells, Name + " / Table cells", diagnostics.ToString());
                        if (mode == OcrMode.TableCells)
                            return new OcrResult(string.Empty, Name, diagnostics.ToString());
                        diagnostics.AppendLine("Auto: no compatible grid found; using text recognition.");
                    }

                    var modes = new[]
                    {
                        PageSegMode.SingleBlock,
                        PageSegMode.Auto,
                        PageSegMode.SparseText
                    };

                    var bestText = string.Empty;
                    var bestScore = int.MinValue;

                    foreach (var preparedPath in preparedPaths.Concat(new[] { imagePath }))
                    foreach (var segmentation in modes)
                    {
                        using (var image = Pix.LoadFromFile(preparedPath))
                        using (var page = engine.Process(image, segmentation))
                        {
                            var text = page.GetText() ?? string.Empty;
                            var score = ScoreText(text, expectedColumns, numbered);
                            diagnostics.AppendLine(Path.GetFileName(preparedPath) + " / " + segmentation + " score=" + score);
                            diagnostics.AppendLine(text);
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

                    return new OcrResult(bestText, Name + " / Text", diagnostics.ToString());
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

        private static string RecognizeCells(TesseractEngine engine, string path, int columns,
            bool numbered, StringBuilder diagnostics)
        {
            using (var bitmap = new Bitmap(path))
            {
                var horizontal = FindGridLines(bitmap, true);
                var vertical = FindGridLines(bitmap, false);
                diagnostics.AppendLine("Grid: " + horizontal.Count + " horizontal, " + vertical.Count + " vertical boundaries.");
                if (vertical.Count != columns + 1 || horizontal.Count < 2)
                {
                    diagnostics.AppendLine("No complete grid matching the selected column layout. Include outer borders and crop unrelated content.");
                    return null;
                }
                var output = new StringBuilder();
                for (var row = 0; row < horizontal.Count - 1; row++)
                {
                    var values = new List<string>();
                    for (var col = 0; col < columns; col++)
                    {
                        // Stay inside the detected line bands, preserving all text in the cell.
                        var left = vertical[col].Item2 + 1;
                        var top = horizontal[row].Item2 + 1;
                        var width = vertical[col + 1].Item1 - left;
                        var height = horizontal[row + 1].Item1 - top;
                        if (width > 0 && height > 0)
                        {
                            var ink = FindInkBounds(bitmap, new Rectangle(left, top, width, height));
                            left = ink.X; top = ink.Y; width = ink.Width; height = ink.Height;
                        }
                        var value = "?";
                        if (width > 3 && height > 3)
                        {
                            using (var cell = new Bitmap(width + 30, height + 30, PixelFormat.Format24bppRgb))
                            {
                                using (var g = Graphics.FromImage(cell))
                                {
                                    g.Clear(Color.White);
                                    g.DrawImage(bitmap, new Rectangle(15, 15, width, height),
                                        new Rectangle(left, top, width, height), GraphicsUnit.Pixel);
                                }
                                using (var stream = new MemoryStream())
                                {
                                    cell.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                                    using (var pix = Pix.LoadFromMemory(stream.ToArray()))
                                    {
                                        string candidate = null;
                                        bool conflict = false;
                                        foreach (var segmentation in new[] { PageSegMode.SingleLine, PageSegMode.SingleWord })
                                        {
                                            using (var page = engine.Process(pix, segmentation))
                                            {
                                                var raw = (page.GetText() ?? string.Empty).Trim();
                                                diagnostics.AppendLine("Row " + (row + 1) + ", column " + (col + 1) + " / " + segmentation + ": " + raw);
                                                // Never join digit groups across a gap: they may be distinct numbers.
                                                var token = Regex.Replace(raw, @"(?<=\d)([.,])\s+(?=\d)", "$1");
                                                var pattern = numbered && col == 0 ? @"^[-+]?\d+$" : @"^[-+]?\d+(?:[.,]\d+)?$";
                                                if (!Regex.IsMatch(token, pattern)) continue;
                                                token = token.Replace(',', '.');
                                                if (candidate == null) candidate = token;
                                                else
                                                {
                                                    decimal a, b;
                                                    if (!decimal.TryParse(candidate, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out a)
                                                        || !decimal.TryParse(token, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out b) || a != b)
                                                        conflict = true;
                                                }
                                            }
                                        }
                                        if (candidate != null && !conflict) value = candidate;
                                        if (conflict) diagnostics.AppendLine("Conflicting readings; cell needs manual review.");
                                    }
                                }
                            }
                        }
                        values.Add(value);
                    }
                    output.AppendLine(string.Join("\t", values));
                }
                diagnostics.AppendLine("Table cells selected. '?' preserves an unreadable or conflicting cell; its row is skipped, never renumbered.");
                return output.ToString();
            }
        }

        private static Rectangle FindInkBounds(Bitmap bitmap, Rectangle area)
        {
            var data = bitmap.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            int minX = area.Width, minY = area.Height, maxX = -1, maxY = -1;
            try
            {
                var pixels = new byte[area.Width * 3];
                for (var y = 0; y < area.Height; y++)
                {
                    Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), pixels, 0, pixels.Length);
                    for (var x = 0; x < area.Width; x++)
                    {
                        if (pixels[x * 3] >= 128) continue;
                        minX = Math.Min(minX, x); minY = Math.Min(minY, y);
                        maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
                    }
                }
            }
            finally { bitmap.UnlockBits(data); }
            return maxX < 0 ? Rectangle.Empty : new Rectangle(area.X + minX, area.Y + minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static List<Tuple<int, int>> FindGridLines(Bitmap bitmap, bool horizontal)
        {
            var length = horizontal ? bitmap.Width : bitmap.Height;
            var count = horizontal ? bitmap.Height : bitmap.Width;
            var hits = new bool[count];
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                var pixels = new byte[data.Stride * bitmap.Height];
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
                for (var i = 0; i < count; i++)
                {
                    int run = 0, longest = 0;
                    for (var j = 0; j < length; j++)
                    {
                        var x = horizontal ? j : i;
                        var y = horizontal ? i : j;
                        run = pixels[y * data.Stride + x * 3] < 128 ? run + 1 : 0;
                        longest = Math.Max(longest, run);
                    }
                    hits[i] = longest >= Math.Max(40, length * 0.6);
                }
            }
            finally { bitmap.UnlockBits(data); }
            var bands = new List<Tuple<int, int>>();
            for (var i = 0; i < count; i++)
            {
                if (!hits[i]) continue;
                var start = i;
                while (i + 1 < count && hits[i + 1]) i++;
                bands.Add(Tuple.Create(start, i));
            }
            return bands;
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
