using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace OCR2Geometry.OCR
{
    // Count fields before any layout-specific missing-Point recovery is applied.
    public static class OcrColumnDetector
    {
        public static bool IsClearlyXyz(string text)
        {
            var first = (text ?? "").Replace("\r", "").Split('\n')
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => Regex.Split(l.Trim(), @"\s+")[0]).ToArray();
            // Positive integer coordinates must never be guessed to be Point IDs.
            return first.Length >= 2 && first.All(v => Regex.IsMatch(v, @"^[+-]?\d+[.,]\d+$"));
        }

        public static int Detect(string text)
        {
            var counts = (text ?? "").Replace("\r", "").Split('\n')
                .Select(line => Regex.Replace(line.Trim(' '), @"(?<=\d)\s*([.,])\s*(?=\d)", "$1"))
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Contains("\t") ? line.Split('\t') : Regex.Split(line.Trim(), @"\s+"))
                .Where(fields => fields.Length >= 2 && fields.Length <= 4 &&
                    fields.All(f => string.IsNullOrWhiteSpace(f) || f == "?" || Regex.IsMatch(f, @"^[+-]?\d+(?:[.,]\d+)?$")))
                .GroupBy(fields => fields.Length).OrderByDescending(group => group.Count()).ToList();
            if (counts.Count == 0 || (counts.Count > 1 && counts[0].Count() == counts[1].Count())) return 0;
            return counts[0].Key;
        }
    }
}
