using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using OCR2Geometry.Models;

namespace OCR2Geometry.AutoCAD
{
    public static class PointCreator
    {
        public static void CreatePoints(IEnumerable<CoordinatePoint> points, double textHeight = 2.5, double textOffset = 2.5, bool createContour = false)
        {
            var rows = points.ToList();
            if (rows.Any(p => p.IsNumberMissing)) throw new InvalidOperationException("Fill all Point numbers before creating geometry.");
            if (rows.Any(p => double.IsNaN(p.X) || double.IsInfinity(p.X) || double.IsNaN(p.Y)
                || double.IsInfinity(p.Y) || double.IsNaN(p.Z) || double.IsInfinity(p.Z)))
                throw new InvalidOperationException("Coordinates must be finite numbers.");
            var vertices = new List<Point3d>();
            if (createContour)
            {
                foreach (var row in rows)
                {
                    var position = new Point3d(row.X, row.Y, row.Z);
                    if (vertices.Count == 0 || !vertices[vertices.Count - 1].Equals(position)) vertices.Add(position);
                }
                if (vertices.Count > 1 && vertices[0].Equals(vertices[vertices.Count - 1])) vertices.RemoveAt(vertices.Count - 1);
                if (vertices.Distinct().Count() < 3)
                    throw new InvalidOperationException("A closed polyline requires at least 3 distinct point positions. Add points or turn off Create closed polyline.");
            }
            var document = Application.DocumentManager.MdiActiveDocument;
            var database = document.Database;

            using (document.LockDocument())
            using (var transaction = database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                foreach (var point in rows)
                {
                    var position = new Point3d(point.X, point.Y, point.Z);

                    var dbPoint = new DBPoint(position);
                    modelSpace.AppendEntity(dbPoint);
                    transaction.AddNewlyCreatedDBObject(dbPoint, true);

                    var label = new DBText
                    {
                        Position = new Point3d(point.X + textOffset, point.Y + textOffset, point.Z),
                        Height = textHeight,
                        TextString = point.Number.ToString()
                    };

                    modelSpace.AppendEntity(label);
                    transaction.AddNewlyCreatedDBObject(label, true);
                }

                if (createContour)
                {
                    if (vertices.All(p => p.Z == vertices[0].Z))
                    {
                        using (var contour = new Polyline(vertices.Count))
                        {
                            contour.SetDatabaseDefaults(database);
                            contour.Elevation = vertices[0].Z;
                            for (var i = 0; i < vertices.Count; i++)
                                contour.AddVertexAt(i, new Point2d(vertices[i].X, vertices[i].Y), 0, 0, 0);
                            contour.Closed = true;
                            modelSpace.AppendEntity(contour);
                            transaction.AddNewlyCreatedDBObject(contour, true);
                        }
                    }
                    else
                    {
                        using (var contour = new Polyline3d(Poly3dType.SimplePoly, new Point3dCollection(vertices.ToArray()), true))
                        {
                            contour.SetDatabaseDefaults(database);
                            modelSpace.AppendEntity(contour);
                            transaction.AddNewlyCreatedDBObject(contour, true);
                        }
                    }
                }

                transaction.Commit();
            }

            document.Editor.Regen();
        }
    }
}
