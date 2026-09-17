using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace OCR2Geometry.OCR
{
    public static class OcrReadingPolicy
    {
        public static string Normalize(string raw, bool pointNumber)
        {
            // Normalize spacing only around an existing decimal mark. Never join digit groups.
            var token = Regex.Replace(raw.Trim(), @"(?<=\d)\s*([.,])\s*(?=\d)", "$1").Replace(',', '.');
            if (!Regex.IsMatch(token, pointNumber ? @"^[-+]?\d+$" : @"^[-+]?\d+(?:\.\d+)?$")) return null;
            if (pointNumber)
            {
                int n;
                return int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)
                    ? n.ToString(CultureInfo.InvariantCulture) : null;
            }
            return token;
        }

        public static string Select(IEnumerable<string> readings, bool pointNumber)
        {
            var valid = readings.Where(r => r != null).ToList();
            if (!pointNumber) return valid.FirstOrDefault() ?? "?";
            var groups = valid.GroupBy(r => r).OrderByDescending(g => g.Count()).ToList();
            if (groups.Count == 0 || groups[0].Count() < 2 ||
                (groups.Count > 1 && groups[0].Count() == groups[1].Count())) return "?";
            return groups[0].Key;
        }
    }
}
