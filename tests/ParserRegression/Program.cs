using System;
using OCR2Geometry.Import;
using OCR2Geometry.OCR;

internal static class Program
{
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        Console.WriteLine("PASS: " + name);
    }

    private static void Main()
    {
        Check(OcrReadingPolicy.Normalize("246443 ,23", false) == "246443.23", "Space before comma from user log");
        Check(OcrReadingPolicy.Normalize("--130579,86", false) == null, "Reject doubled sign");
        Check(OcrReadingPolicy.Normalize("246443 23", false) == null, "Do not join separated digits");
        Check(OcrReadingPolicy.Select(new[] { "-128368.72", "12836872" }, false) == "-128368.72", "Secondary reading cannot veto primary sign/decimal");
        Check(OcrReadingPolicy.Select(new[] { "8" }, true) == "?", "Single unsupported point read is rejected");
        Check(OcrReadingPolicy.Select(new[] { "3", "3", "8" }, true) == "3", "Point retries agree");
        Check(OcrReadingPolicy.Select(new[] { "3", "8" }, true) == "?", "Conflicting point reads remain unresolved");
        const string actualLog = "252956,80 -128368,72 725,50\n2 250123,60 -124965,21 786,00\n3 247916,00 -123185,69 865,70\n4 246443 ,23 -125053,95 800,00\n5 250864,04 -130579,84 445,00";
        var retained = TextCoordinateParser.ParseOcr(actualLog, 1);
        Check(retained.Points.Count == 5 && retained.Points[0].Number == null && retained.Points[0].X == 252956.80
            && retained.Points[0].Y == -128368.72 && retained.Points[0].Z == 725.50
            && retained.Points[1].Number == 2 && retained.ReviewDetails.Count == 1, "Actual user log retains all five rows with blank first Point");
        retained.Points[0].Number = 1;
        Check(!retained.Points[0].IsNumberMissing, "Manual Point entry clears missing flag");
        var emptyCell = TextCoordinateParser.ParseOcr("\t252956,80\t-128368,72\t725,50", 1);
        Check(emptyCell.Points.Count == 1 && emptyCell.Points[0].IsNumberMissing, "Empty first tab cell preserves coordinates");
        var ambiguous = TextCoordinateParser.ParseOcr("2 250123 786", 1);
        Check(ambiguous.Points.Count == 0, "Ambiguous integer-leading short row is not shifted");
        var missingXy = TextCoordinateParser.ParseOcr("?\t252956\t-128368", 1, 3, true);
        Check(missingXy.Points.Count == 1 && missingXy.Points[0].IsNumberMissing && missingXy.Points[0].Z == 0, "Grid Point XY retains missing number");
        var spaced = TextCoordinateParser.ParseOcr("4 246443 ,23 -125053,95 800,00", 1);
        Check(spaced.Points.Count == 1 && spaced.Points[0].X == 246443.23, "Text mode handles space before comma");
        var cells = TextCoordinateParser.ParseOcr("1\t252956,80\t-128368,72\t725,50\n2\t250123,60\t-124965,21\t786,00", 1);
        Check(cells.Points.Count == 2 && cells.Points[0].Number == 1 && cells.Points[0].Y == -128368.72, "First grid row and negative coordinates survive");
        var missing = TextCoordinateParser.ParseOcr("?\t252956,80\t-128368,72\t725,50\n2\t?\t-124965,21\t786,00", 1);
        Check(missing.Points.Count == 1 && missing.Points[0].IsNumberMissing && missing.Points[0].X == 252956.80 && missing.InvalidLineDetails.Count == 1, "Missing Point retains coordinates; missing X still rejected");
        var nonfinite = TextCoordinateParser.ParseOcr("1\tNaN\t12\t13", 1);
        Check(nonfinite.Points.Count == 0, "Nonfinite values rejected");
        const string rows = "1 72968,58 65990,98 725,50\n2 70174,18 69426,41 786,00\n3 68041,52 71178,59 865,70\n4 66510,06 69358,06 800,00\n5 70850,81 63803,75 445,00";
        var parsed = TextCoordinateParser.ParseOcr(rows, 1);
        Check(parsed.Points.Count == 5 && parsed.Points[0].Number == 1 &&
            parsed.Points[0].X == 72968.58 && parsed.Points[4].Y == 63803.75 &&
            parsed.Points[2].Z == 865.70, "Screenshot reference rows retain Point/X/Y/Z");
        var recovered = TextCoordinateParser.ParseOcr(rows.Replace("70174,18", "7017418"), 1);
        Check(recovered.Points[1].X == 70174.18 && recovered.Points[1].IsXRecovered &&
            recovered.RecoveredDecimalCount == 1, "Missing separator is recovered and highlighted");
        var broken = TextCoordinateParser.ParseOcr("72968,58 65990,98 725,50\n1 72968 58 65990,98 725,50", 1);
        Check(broken.Points.Count == 1 && broken.Points[0].IsNumberMissing && broken.InvalidLineNumbers.Count == 1,
            "Missing Point retained; extra numeric fragments rejected");
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
