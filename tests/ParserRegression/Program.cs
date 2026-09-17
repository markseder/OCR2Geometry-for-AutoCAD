using System;
using OCR2Geometry.Import;

internal static class Program
{
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        Console.WriteLine("PASS: " + name);
    }

    private static void Main()
    {
        const string rows = "1 72968,58 65990,98 725,50\n2 70174,18 69426,41 786,00\n3 68041,52 71178,59 865,70\n4 66510,06 69358,06 800,00\n5 70850,81 63803,75 445,00";
        var parsed = TextCoordinateParser.ParseOcr(rows, 1);
        Check(parsed.Points.Count == 5 && parsed.Points[0].Number == 1 &&
            parsed.Points[0].X == 72968.58 && parsed.Points[4].Y == 63803.75 &&
            parsed.Points[2].Z == 865.70, "Screenshot reference rows retain Point/X/Y/Z");
        var recovered = TextCoordinateParser.ParseOcr(rows.Replace("70174,18", "7017418"), 1);
        Check(recovered.Points[1].X == 70174.18 && recovered.Points[1].IsXRecovered &&
            recovered.RecoveredDecimalCount == 1, "Missing separator is recovered and highlighted");
        var broken = TextCoordinateParser.ParseOcr("72968,58 65990,98 725,50\n1 72968 58 65990,98 725,50", 1);
        Check(broken.Points.Count == 0 && broken.InvalidLineNumbers.Count == 2,
            "Missing Point and extra numeric fragments are rejected");
        var xyz = TextCoordinateParser.ParseOcr("72968 65990 725", 10, 3, false);
        Check(xyz.Points[0].Number == 10 && xyz.Points[0].X == 72968 && xyz.Points[0].Z == 725,
            "Integer X is not mistaken for Point in XYZ mode");
        var xy = TextCoordinateParser.ParseOcr("72968,58 65990,98", 10, 2, false);
        Check(xy.Points[0].Number == 10 && xy.Points[0].Z == 0, "XY mode supplies Z zero");
        var pointXy = TextCoordinateParser.ParseOcr("20 72968,58 65990,98", 1, 3, true);
        Check(pointXy.Points[0].Number == 20 && pointXy.Points[0].Z == 0, "Point XY mode");
        var comma = TextCoordinateParser.ParseOcr("1 72968,58 65990,98 725, 50", 1);
        Check(comma.Points[0].Z == 725.50, "Whitespace after existing decimal separator");
        var csv = TextCoordinateParser.Parse("Point,X,Y,Z\n1,72968.58,65990.98,725.50", 1);
        Check(csv.Points.Count == 1 && csv.Points[0].X == 72968.58, "CSV import remains supported");
        var untouched = TextCoordinateParser.Parse(rows.Replace("70174,18", "7017418"), 1);
        Check(untouched.Points[1].X == 7017418 && !untouched.Points[1].IsXRecovered,
            "Text import never repairs decimal separators");
        var weak = TextCoordinateParser.ParseOcr("1 70174,18 69426,41 786,00\n2 7017418 69426,41 786,00", 1);
        Check(!weak.Points[1].IsXRecovered, "One sample is insufficient for automatic recovery");
    }
}
