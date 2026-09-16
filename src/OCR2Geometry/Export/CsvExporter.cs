using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using OCR2Geometry.Models;

namespace OCR2Geometry.Export
{
    public static class CsvExporter
    {
        public static void Export(string path, IEnumerable<CoordinatePoint> points)
        {
            using (var writer = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                writer.WriteLine("Point,X,Y");

                foreach (var point in points)
                {
                    writer.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0},{1:0.########},{2:0.########}",
                        point.Number,
                        point.X,
                        point.Y));
                }
            }
        }
    }
}
