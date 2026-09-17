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
                preparedPaths.Add(PrepareForOcr(imagePath, false, false));
                using (var engine = new TesseractEngine(_tessdataDirectory, Language, EngineMode.LstmOnly))
                {
                    engine.SetVariable("tessedit_char_whitelist", "0123456789.,-+ ");
                    engine.SetVariable("preserve_interword_spaces", "1");
                    engine.SetVariable("classify_bln_numeric_mode", "1");
                    engine.SetVariable("user_defined_dpi", "300");

                    var diagnostics = new StringBuilder();
                    diagnostics.AppendLine("Requested mode: " + mode);
                    string cells = null;
                    if (mode != OcrMode.Text)
                    {
                        cells = RecognizeCells(engine, preparedPaths[1], preparedPaths[2], expectedColumns, numbered, diagnostics);
                        if (cells != null && mode == OcrMode.TableCells) return new OcrResult(cells, Name + " / Table cells", diagnostics.ToString());
                        if (mode == OcrMode.TableCells)
                            return new OcrResult(string.Empty, Name, diagnostics.ToString());
                        diagnostics.AppendLine("Auto: compare cell result with text recognition; never stop on an empty cell result.");
                    }

                    var modes = new[]
                    {
                        PageSegMode.SingleBlock,
                        PageSegMode.Auto,
                        PageSegMode.SparseText
                    };

                    var bestText = cells ?? string.Empty;
                    var bestScore = cells == null ? int.MinValue : ScoreText(cells, expectedColumns, numbered);
                    var selectedMethod = cells == null ? "Text" : "Table cells";

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
                                selectedMethod = "Text";
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

                    diagnostics.AppendLine("Auto/text selection: " + selectedMethod + "; score=" + bestScore);
                    return new OcrResult(bestText, Name + " / " + selectedMethod, diagnostics.ToString());
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

        private static string RecognizeCells(TesseractEngine engine, string path, string grayscalePath, int columns,
            bool numbered, StringBuilder diagnostics)
        {
            using (var bitmap = new Bitmap(path))
            using (var grayscale = new Bitmap(grayscalePath))
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
                            value = ReadCell(engine, bitmap, grayscale, new Rectangle(left, top, width, height),
                                numbered && col == 0, row + 1, col + 1, diagnostics);
                        }
                        values.Add(value);
                    }
                    output.AppendLine(string.Join("\t", values));
                }
                diagnostics.AppendLine("Table cells: '?' denotes unreadable/unconfirmed content. Review highlighted OCR rows against the source; point numbers are never inferred.");
                return output.ToString();
            }
        }

        private static string ReadCell(TesseractEngine engine, Bitmap binary, Bitmap grayscale,
            Rectangle bounds, bool pointNumber, int row, int column, StringBuilder diagnostics)
        {
            var readings = new List<string>();
            // Coordinates: prefer a complete line, preserving signs and decimal marks.
            // SingleWord is deliberately excluded: it dropped punctuation in the user's log.
            var modes = pointNumber
                ? new[] { PageSegMode.SingleBlock, PageSegMode.SingleChar, PageSegMode.SingleWord }
                : new[] { PageSegMode.SingleLine, PageSegMode.SingleBlock };
            foreach (var source in new[] { binary, grayscale })
            using (var cell = new Bitmap(bounds.Width + 40, bounds.Height + 40, PixelFormat.Format24bppRgb))
            {
                using (var g = Graphics.FromImage(cell))
                {
                    g.Clear(Color.White);
                    g.DrawImage(source, new Rectangle(20, 20, bounds.Width, bounds.Height), bounds, GraphicsUnit.Pixel);
                }
                using (var stream = new MemoryStream())
                {
                    cell.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    using (var pix = Pix.LoadFromMemory(stream.ToArray()))
                    foreach (var segmentation in modes)
                    using (var page = engine.Process(pix, segmentation))
                    {
                        var raw = (page.GetText() ?? string.Empty).Trim();
                        var token = OcrReadingPolicy.Normalize(raw, pointNumber);
                        diagnostics.AppendLine("Row " + row + ", column " + column + " / "
                            + (source == binary ? "binary" : "grayscale") + " / " + segmentation + ": " + raw);
                        if (token != null) readings.Add(token);
                    }
                }
            }
            var selected = OcrReadingPolicy.Select(readings, pointNumber);
            diagnostics.AppendLine("Selected: " + selected + (pointNumber
                ? " (requires at least two agreeing reads and a unique winner)"
                : " (first valid SingleLine/SingleBlock; verify against image)"));
            if (readings.Distinct().Count() > 1)
                diagnostics.AppendLine("Alternate readings differ; OCR row is highlighted for review.");
            return selected;
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
                - parsed.RecoveredDecimalCount * 10 - parsed.Points.Count(p => p.IsNumberMissing) * 20;
        }

        private static string PrepareForOcr(string sourcePath, bool removeGrid, bool threshold = true)
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
                        if (threshold) attributes.SetThreshold(0.72f);

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
