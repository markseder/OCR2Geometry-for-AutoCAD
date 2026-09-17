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

                double x;
                double y;
                if (!TryParseDouble(values[xIndex], out x) || !TryParseDouble(values[yIndex], out y))
                {
                    result.InvalidLineNumbers.Add(i + 1);
                    continue;
                }

                var z = 0.0;
                if (values.Count > zIndex && !TryParseDouble(values[zIndex], out z))
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

        private static bool HasExplicitPointNumber(List<string> values, out int pointNumber)
        {
            pointNumber = 0;
            if (values.Count < 3 || !TryParseInt(values[0], out pointNumber))
            {
                return false;
            }

            // Four values naturally map to Point,X,Y,Z.
            if (values.Count >= 4)
            {
                return true;
            }

            // Three values are ambiguous: they can be Point,X,Y or X,Y,Z.
            // Treat a reasonably sized leading integer as a point number; large
            // coordinate-like values are treated as X so XYZ rows without point
            // numbers also work in the common surveying case.
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

            // CSV exported by this plugin uses commas as separators. Treat compact
            // lines with at least two commas and no whitespace as CSV. This keeps
            // rows such as 102,0,0 and 102,0,0,0 unambiguous.
            var commaCount = line.Count(c => c == ',');
            if (commaCount >= 2 && !line.Any(char.IsWhiteSpace))
            {
                return line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim())
                    .Where(v => v.Length > 0)
                    .ToList();
            }

            // Whitespace-separated OCR/text can use either decimal dots or decimal commas.
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
