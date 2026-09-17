using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using OCR2Geometry.Models;

namespace OCR2Geometry.Import
{
    public sealed class ParseResult
    {
        public List<CoordinatePoint> Points { get; } = new List<CoordinatePoint>();
        public List<string> InvalidLineDetails { get; } = new List<string>();
        public List<int> InvalidLineNumbers { get; } = new List<int>();
        public List<string> ReviewDetails { get; } = new List<string>();
        public int RecoveredDecimalCount { get; set; }
    }

    public static class TextCoordinateParser
    {
        private static readonly Regex NumberRegex = new Regex(@"[-+]?\d+(?:[\.,]\d+)?", RegexOptions.Compiled);

        private sealed class OcrColumnProfile
        {
            public int IntegerDigits { get; set; }
            public int DecimalPlaces { get; set; }
            public bool IsValid { get; set; }
        }

        public static ParseResult Parse(string text, int startNumber)
        {
            return Parse(text, startNumber, false);
        }

        public static ParseResult ParseOcr(string text, int startNumber, int expectedColumns = 4, bool numbered = true)
        {
            return Parse(text, startNumber, true, expectedColumns, numbered);
        }

        private static ParseResult Parse(string text, int startNumber, bool recoverOcrDecimals, int expectedColumns = 0, bool numbered = false)
        {
            var result = new ParseResult();
            if (string.IsNullOrWhiteSpace(text))
            {
                return result;
            }

            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var profiles = recoverOcrDecimals ? BuildOcrColumnProfiles(lines, expectedColumns, numbered) : null;
            var nextNumber = startNumber;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = recoverOcrDecimals ? lines[i].Trim(' ') : lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var lower = line.ToLowerInvariant();
                if ((lower.Contains("point") || lower.Contains("точ")) && lower.Contains("x") && lower.Contains("y"))
                {
                    continue;
                }

                var values = recoverOcrDecimals ? GetOcrValues(line, expectedColumns, numbered) : ExtractValues(line);
                if (recoverOcrDecimals && values.Count != expectedColumns)
                {
                    result.InvalidLineNumbers.Add(i + 1);
                    result.InvalidLineDetails.Add("Line " + (i + 1) + ": " + line + " — expected " + expectedColumns + " fields, found " + values.Count + ".");
                    continue;
                }
                if (values.Count < 2)
                {
                    result.InvalidLineNumbers.Add(i + 1);
                    result.InvalidLineDetails.Add("Line " + (i + 1) + ": " + line + " — fewer than two coordinate fields.");
                    continue;
                }

                var explicitNumber = 0;
                var hasExplicitNumber = recoverOcrDecimals ? numbered : HasExplicitPointNumber(values, out explicitNumber);
                var numberMissing = recoverOcrDecimals && numbered && !TryParseInt(values[0], out explicitNumber);
                var xIndex = hasExplicitNumber ? 1 : 0;
                var yIndex = hasExplicitNumber ? 2 : 1;
                var zIndex = hasExplicitNumber ? 3 : 2;

                if (values.Count <= yIndex)
                {
                    result.InvalidLineNumbers.Add(i + 1);
                    result.InvalidLineDetails.Add("Line " + (i + 1) + ": " + line + " — X or Y column missing.");
                    continue;
                }

                var xToken = values[xIndex];
                var yToken = values[yIndex];
                var zToken = values.Count > zIndex ? values[zIndex] : null;

                var xRecovered = false;
                var yRecovered = false;
                var zRecovered = false;

                if (recoverOcrDecimals)
                {
                    xToken = RecoverMissingDecimal(xToken, profiles[0], result, out xRecovered);
                    yToken = RecoverMissingDecimal(yToken, profiles[1], result, out yRecovered);
                    if (zToken != null)
                    {
                        zToken = RecoverMissingDecimal(zToken, profiles[2], result, out zRecovered);
                    }
                }

                double x;
                double y;
                if (!TryParseDouble(xToken, out x) || !TryParseDouble(yToken, out y))
                {
                    result.InvalidLineNumbers.Add(i + 1);
                    result.InvalidLineDetails.Add("Line " + (i + 1) + ": " + line + " — X or Y is unreadable or invalid: [" + xToken + "] [" + yToken + "]");
                    continue;
                }

                var z = 0.0;
                if (zToken != null && !TryParseDouble(zToken, out z))
                {
                    result.InvalidLineNumbers.Add(i + 1);
                    result.InvalidLineDetails.Add("Line " + (i + 1) + ": " + line + " — Z is unreadable or invalid: [" + zToken + "]");
                    continue;
                }

                int? number = numberMissing ? (int?)null : hasExplicitNumber ? explicitNumber : nextNumber;
                var point = new CoordinatePoint(number, x, y, z)
                {
                    IsXRecovered = xRecovered,
                    IsYRecovered = yRecovered,
                    IsZRecovered = zRecovered
                };

                result.Points.Add(point);
                if (numberMissing) result.ReviewDetails.Add("Line " + (i + 1) + ": coordinates retained; enter Point manually.");
                if (number.HasValue && number.Value < int.MaxValue) nextNumber = number.Value + 1;
            }

            return result;
        }

        private static OcrColumnProfile[] BuildOcrColumnProfiles(string[] lines, int expectedColumns, bool numbered)
        {
            var samples = new[]
            {
                new List<Tuple<int, int>>(),
                new List<Tuple<int, int>>(),
                new List<Tuple<int, int>>()
            };

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = GetOcrValues(line, expectedColumns, numbered);
                if (values.Count != expectedColumns)
                {
                    continue;
                }

                int pointNumber;
                var hasExplicitNumber = numbered;
                if (numbered && !TryParseInt(values[0], out pointNumber)) continue;
                var firstCoordinate = hasExplicitNumber ? 1 : 0;

                for (var column = 0; column < 3; column++)
                {
                    var valueIndex = firstCoordinate + column;
                    if (valueIndex >= values.Count)
                    {
                        continue;
                    }

                    int integerDigits;
                    int decimalPlaces;
                    if (TryGetDecimalShape(values[valueIndex], out integerDigits, out decimalPlaces))
                    {
                        samples[column].Add(Tuple.Create(integerDigits, decimalPlaces));
                    }
                }
            }

            var profiles = new OcrColumnProfile[3];
            for (var column = 0; column < 3; column++)
            {
                profiles[column] = BuildProfile(samples[column]);
            }

            return profiles;
        }

        private static OcrColumnProfile BuildProfile(List<Tuple<int, int>> samples)
        {
            var profile = new OcrColumnProfile();
            if (samples.Count < 2)
            {
                return profile;
            }

            var mostCommonDecimalPlaces = samples
                .GroupBy(s => s.Item2)
                .OrderByDescending(g => g.Count())
                .ThenByDescending(g => g.Key)
                .First().Key;

            var matching = samples.Where(s => s.Item2 == mostCommonDecimalPlaces).ToList();
            var mostCommonIntegerDigits = matching
                .GroupBy(s => s.Item1)
                .OrderByDescending(g => g.Count())
                .ThenByDescending(g => g.Key)
                .First().Key;

            profile.DecimalPlaces = mostCommonDecimalPlaces;
            profile.IntegerDigits = mostCommonIntegerDigits;
            profile.IsValid = mostCommonDecimalPlaces > 0 &&
                matching.Count(s => s.Item1 == mostCommonIntegerDigits) >= 2 &&
                matching.Count(s => s.Item1 == mostCommonIntegerDigits) * 2 > samples.Count;
            return profile;
        }

        private static bool TryGetDecimalShape(string token, out int integerDigits, out int decimalPlaces)
        {
            integerDigits = 0;
            decimalPlaces = 0;

            var trimmed = token.Trim();
            var separatorIndex = Math.Max(trimmed.LastIndexOf('.'), trimmed.LastIndexOf(','));
            if (separatorIndex <= 0 || separatorIndex >= trimmed.Length - 1)
            {
                return false;
            }

            var integerPart = trimmed.Substring(0, separatorIndex).TrimStart('+', '-');
            var decimalPart = trimmed.Substring(separatorIndex + 1);
            if (integerPart.Length == 0 || decimalPart.Length == 0 ||
                !integerPart.All(char.IsDigit) || !decimalPart.All(char.IsDigit))
            {
                return false;
            }

            integerDigits = integerPart.Length;
            decimalPlaces = decimalPart.Length;
            return decimalPlaces <= 4;
        }

        private static string RecoverMissingDecimal(
            string token,
            OcrColumnProfile profile,
            ParseResult result,
            out bool recovered)
        {
            recovered = false;

            if (profile == null || !profile.IsValid || string.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            var trimmed = token.Trim();
            if (trimmed.Contains(".") || trimmed.Contains(","))
            {
                return token;
            }

            var sign = string.Empty;
            var digits = trimmed;
            if (digits.StartsWith("+") || digits.StartsWith("-"))
            {
                sign = digits.Substring(0, 1);
                digits = digits.Substring(1);
            }

            if (digits.Length != profile.IntegerDigits + profile.DecimalPlaces || !digits.All(char.IsDigit))
            {
                return token;
            }

            var splitIndex = digits.Length - profile.DecimalPlaces;
            if (splitIndex <= 0)
            {
                return token;
            }

            recovered = true;
            result.RecoveredDecimalCount++;
            return sign + digits.Substring(0, splitIndex) + "." + digits.Substring(splitIndex);
        }

        private static bool HasExplicitPointNumber(List<string> values, out int pointNumber)
        {
            pointNumber = 0;
            if (values.Count < 3 || !TryParseInt(values[0], out pointNumber))
            {
                return false;
            }

            if (values.Count >= 4)
            {
                return true;
            }

            return Math.Abs((long)pointNumber) < 100000;
        }

        private static List<string> GetOcrValues(string line, int expectedColumns, bool numbered)
        {
            var values = ExtractOcrValues(line);
            int ignored;
            // Only unambiguous decimal-leading text rows may omit Point.
            // A leading integer could be Point with a missing coordinate: do not shift it.
            if (numbered && !line.Contains("\t") && values.Count == expectedColumns - 1
                && values.Count > 0 && !TryParseInt(values[0], out ignored)) values.Insert(0, "?");
            return values;
        }

        private static List<string> ExtractOcrValues(string line)
        {
            // Tabs from cell OCR are hard column boundaries, including empty cells.
            if (line.Contains("\t")) return line.Split(new[] { '\t' }, StringSplitOptions.None).Select(v => v.Trim()).ToList();
            // A comma is a decimal separator in OCR, never a CSV delimiter.
            // Tesseract may insert whitespace between a separator and its digits.
            var normalized = Regex.Replace(line, @"(?<=\d)\s*([.,])\s*(?=\d{1,4}(?:\s|$))", "$1");
            return NumberRegex.Matches(normalized).Cast<Match>().Select(m => m.Value).ToList();
        }

        private static List<string> ExtractValues(string line)
        {
            if (line.Contains("\t"))
            {
                return line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim())
                    .Where(v => v.Length > 0)
                    .ToList();
            }

            if (line.Contains(";"))
            {
                return line.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim())
                    .Where(v => v.Length > 0)
                    .ToList();
            }

            var commaCount = line.Count(c => c == ',');
            if (commaCount >= 2 && !line.Any(char.IsWhiteSpace))
            {
                return line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim())
                    .Where(v => v.Length > 0)
                    .ToList();
            }

            return NumberRegex.Matches(line).Cast<Match>().Select(m => m.Value).ToList();
        }

        private static bool TryParseDouble(string value, out double result)
        {
            var normalized = value.Trim().Replace(',', '.');
            return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out result)
                && !double.IsNaN(result) && !double.IsInfinity(result);
        }

        private static bool TryParseInt(string value, out int result)
        {
            return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }
    }
}
