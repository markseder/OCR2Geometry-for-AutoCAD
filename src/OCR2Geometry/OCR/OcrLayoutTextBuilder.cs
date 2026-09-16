using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RapidOCRLib.Models;

namespace OCR2Geometry.OCR
{
    public static class OcrLayoutTextBuilder
    {
        private sealed class BlockInfo
        {
            public TextBlock Block { get; set; }
            public double CenterX { get; set; }
            public double CenterY { get; set; }
            public double Height { get; set; }
        }

        public static string BuildRows(OcrResult result)
        {
            if (result == null || result.TextBlocks == null || result.TextBlocks.Count == 0)
            {
                return string.Empty;
            }

            var blocks = result.TextBlocks
                .Where(b => b != null && !string.IsNullOrWhiteSpace(b.Text) && b.BoxPoints != null && b.BoxPoints.Count >= 4)
                .Select(ToBlockInfo)
                .OrderBy(b => b.CenterY)
                .ThenBy(b => b.CenterX)
                .ToList();

            if (blocks.Count == 0)
            {
                return string.Empty;
            }

            var medianHeight = Median(blocks.Select(b => b.Height).Where(h => h > 0).ToList());
            var rowTolerance = Math.Max(6.0, medianHeight * 0.65);
            var rows = new List<List<BlockInfo>>();

            foreach (var block in blocks)
            {
                List<BlockInfo> bestRow = null;
                var bestDistance = double.MaxValue;

                foreach (var row in rows)
                {
                    var rowY = row.Average(item => item.CenterY);
                    var distance = Math.Abs(block.CenterY - rowY);
                    if (distance <= rowTolerance && distance < bestDistance)
                    {
                        bestRow = row;
                        bestDistance = distance;
                    }
                }

                if (bestRow == null)
                {
                    rows.Add(new List<BlockInfo> { block });
                }
                else
                {
                    bestRow.Add(block);
                }
            }

            var orderedRows = rows
                .OrderBy(row => row.Average(item => item.CenterY))
                .ToList();

            var builder = new StringBuilder();
            foreach (var row in orderedRows)
            {
                var cells = row
                    .OrderBy(item => item.CenterX)
                    .Select(item => item.Block.Text.Trim())
                    .Where(text => text.Length > 0)
                    .ToList();

                if (cells.Count == 0)
                {
                    continue;
                }

                builder.AppendLine(string.Join("\t", cells));
            }

            return builder.ToString();
        }

        private static BlockInfo ToBlockInfo(TextBlock block)
        {
            var xs = block.BoxPoints.Select(p => (double)p.X).ToList();
            var ys = block.BoxPoints.Select(p => (double)p.Y).ToList();

            return new BlockInfo
            {
                Block = block,
                CenterX = xs.Average(),
                CenterY = ys.Average(),
                Height = ys.Max() - ys.Min()
            };
        }

        private static double Median(List<double> values)
        {
            if (values == null || values.Count == 0)
            {
                return 20.0;
            }

            values.Sort();
            var middle = values.Count / 2;
            if (values.Count % 2 == 0)
            {
                return (values[middle - 1] + values[middle]) / 2.0;
            }

            return values[middle];
        }
    }
}
