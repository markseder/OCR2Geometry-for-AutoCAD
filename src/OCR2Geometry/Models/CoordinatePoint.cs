namespace OCR2Geometry.Models
{
    public sealed class CoordinatePoint
    {
        public int Number { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public CoordinatePoint(int number, double x, double y)
        {
            Number = number;
            X = x;
            Y = y;
        }
    }
}
