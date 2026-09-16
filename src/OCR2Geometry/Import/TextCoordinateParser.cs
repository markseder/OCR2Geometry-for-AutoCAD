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
    }

    public static class TextCoordinateParser
    {
        private static readonly Regex NumberRegex = new Regex(@"[-+]?\d+(?:[\.,]\d+)?", RegexOptions.Compiled);

        public static ParseResult Parse(string text, int startNumber)
        {
            var result = new ParseResult();
            if (string.IsNullOrWhiteSpace(text))
            {
                return result;
            }

            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
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

                var matches = NumberRegex.Matches(line).Cast<Match>().Select(m => m.Value).ToList();
                if (matches.Count < 2)
                {
                    result.InvalidLineNumbers.Add(i + 1);
                    continue;
                }

                double x;
                double y;
                int explicitNumber;
                var hasExplicitNumber = matches.Count >= 3 && TryParseInt(matches[0], out explicitNumber);
                var xIndex = hasExplicitNumber ? 1 : 0;
                var yIndex = hasExplicitNumber ? 2 : 1;

                if (!TryParseDouble(matches[xIndex], out x) || !TryParseDouble(matches[yIndex], out y))
                {
                    result.InvalidLineNumbers.Add(i + 1);
                    continue;
                }

                var number = hasExplicitNumber ? explicitNumber : nextNumber;
                result.Points.Add(new CoordinatePoint(number, x, y));
                nextNumber = number + 1;
            }

            return result;
        }

        private static bool TryParseDouble(string value, out double result)
        {
            var normalized = value.Replace(',', '.');
            return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        private static bool TryParseInt(string value, out int result)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }
    }
}
