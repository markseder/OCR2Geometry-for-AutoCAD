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
        public List<int> InvalidLineNumbers { get; } = new List<int>();
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

        public static ParseResult ParseOcr(string text, int startNumber)
        {
            return Parse(text, startNumber, true);
        }

        private static ParseResult Parse(string text, int startNumber, bool recoverOcrDecimals)
        {
            var result = new ParseResult();
            if (string.IsNullOrWhiteSpace(text))
            {
                return result;
            }

            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var profiles = recoverOcrDecimals ? BuildOcrColumnProfiles(lines) : null;
            var nextNumber = startNumber;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var lower = line.ToLowerInvariant();
                if ((lower.Contains("point") || lower.Contains("точ")) && lower.Contains("x") && lower.Contains("y"))
                {
                    continue;
                }

                var values = ExtractValues(line);
                if (values.Count < 2)
                {
                    result.InvalidLineNumbers.Add(i + 1);
                    continue;
                }

                var explicitNumber = 0;
                var hasExplicitNumber = HasExplicitPointNumber(values, out explicitNumber);
                var xIndex = hasExplicitNumber ? 1 : 0;
                var yIndex = hasExplicitNumber ? 2 : 1;
                var zIndex = hasExplicitNumber ? 3 : 2;

                if (values.Count <= yIndex)
                {
                    result.InvalidLineNumbers.Add(i + 1);
                    continue;
                }

                var xToken = values[xIndex];
                var yToken = values[yIndex];
                var zToken = values.Count > zIndex ? values[zIndex] : null;

                if (recoverOcrDecimals)
                {
                    xToken = RecoverMissingDecimal(xToken, profiles[0], result);
                    yToken = RecoverMissingDecimal(yToken, profiles[1], result);
                    if (zToken != null)
                    {
                        zToken = RecoverMissingDecimal(zToken, profiles[2], result);
                    }
                }

                double x;
                double y;
                if (!TryParseDouble(xToken, out x) || !TryParseDouble(yToken, out y))
                {
                    result.InvalidLineNumbers.Add(i + 1);
                    continue;
                }

                var z = 0.0;
                if (zToken != null && !TryParseDouble(zToken, out z))
                {
                    result.InvalidLineNumbers.Add(i + 1);
                    continue;
                }

                var number = hasExplicitNumber ? explicitNumber : nextNumber;
                result.Points.Add(new CoordinatePoint(number, x, y, z));
                nextNumber = number + 1;
            }

            return result;
        }

        private static OcrColumnProfile[] BuildOcrColumnProfiles(string[] lines)
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

                var values = ExtractValues(line);
                if (values.Count < 2)
                {
                    continue;
                }

                int pointNumber;
                var hasExplicitNumber = HasExplicitPointNumber(values, out pointNumber);
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
            if (samples.Count == 0)
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
            profile.IsValid = mostCommonDecimalPlaces > 0;
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

        private static string RecoverMissingDecimal(string token, OcrColumnProfile profile, ParseResult result)
        {
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
            return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        private static bool TryParseInt(string value, out int result)
        {
            return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }
    }
}
