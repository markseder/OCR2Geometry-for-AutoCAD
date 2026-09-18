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
        private static ObjectId GetOrCreateLayer(Database database, Transaction transaction, string name)
        {
            var table = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
            if (table.Has(name))
            {
                var layer = (LayerTableRecord)transaction.GetObject(table[name], OpenMode.ForRead);
                if (layer.IsLocked) throw new InvalidOperationException("Layer '" + name + "' is locked. Unlock it before creating objects.");
                return layer.ObjectId;
            }
            if (!table.IsWriteEnabled) table.UpgradeOpen();
            var newLayer = new LayerTableRecord { Name = name };
            var id = table.Add(newLayer);
            transaction.AddNewlyCreatedDBObject(newLayer, true);
            return id;
        }

        public static void CreatePoints(IEnumerable<CoordinatePoint> points, double textHeight = 2.5, double textOffset = 2.5, bool createContour = false, bool createPoints = true, bool createLabels = true, bool closeContour = true)
        {
            var rows = points.ToList();
            if (!createPoints && !createLabels && !createContour) throw new InvalidOperationException("Select Points, Labels or Polyline before creating objects.");
            if (rows.Count == 0) throw new InvalidOperationException("The coordinate table is empty.");
            if (createLabels && (textHeight <= 0 || double.IsNaN(textHeight) || double.IsInfinity(textHeight)))
                throw new InvalidOperationException("Text height must be a finite positive number.");
            if (createLabels && rows.Any(p => p.IsNumberMissing)) throw new InvalidOperationException("Fill all Point numbers before creating labels.");
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
                if (closeContour && vertices.Count > 1 && vertices[0].Equals(vertices[vertices.Count - 1])) vertices.RemoveAt(vertices.Count - 1);
                if (vertices.Distinct().Count() < (closeContour ? 3 : 2))
                    throw new InvalidOperationException("A " + (closeContour ? "closed polyline requires at least 3" : "open polyline requires at least 2") + " distinct point positions.");
            }
            var document = Application.DocumentManager.MdiActiveDocument;
            var database = document.Database;

            using (document.LockDocument())
            using (var transaction = database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var pointLayer = createPoints ? GetOrCreateLayer(database, transaction, "Points") : ObjectId.Null;
                var labelLayer = createLabels ? GetOrCreateLayer(database, transaction, "Labels") : ObjectId.Null;
                var polylineLayer = createContour ? GetOrCreateLayer(database, transaction, "Polyline") : ObjectId.Null;

                foreach (var point in rows)
                {
                    var position = new Point3d(point.X, point.Y, point.Z);

                    if (createPoints)
                    {
                        var dbPoint = new DBPoint(position);
                        dbPoint.SetDatabaseDefaults(database);
                        dbPoint.LayerId = pointLayer;
                        modelSpace.AppendEntity(dbPoint);
                        transaction.AddNewlyCreatedDBObject(dbPoint, true);
                    }
                    if (createLabels)
                    {
                        var label = new DBText();
                        label.SetDatabaseDefaults(database);
                        label.LayerId = labelLayer;
                        label.Position = new Point3d(point.X + textOffset, point.Y + textOffset, point.Z);
                        label.Height = textHeight;
                        label.TextString = point.Number.ToString();
                        modelSpace.AppendEntity(label);
                        transaction.AddNewlyCreatedDBObject(label, true);
                    }
                }

                if (createContour)
                {
                    if (vertices.All(p => p.Z == vertices[0].Z))
                    {
                        using (var contour = new Polyline(vertices.Count))
                        {
                            contour.SetDatabaseDefaults(database);
                            contour.LayerId = polylineLayer;
                            contour.Elevation = vertices[0].Z;
                            for (var i = 0; i < vertices.Count; i++)
                                contour.AddVertexAt(i, new Point2d(vertices[i].X, vertices[i].Y), 0, 0, 0);
                            contour.Closed = closeContour;
                            modelSpace.AppendEntity(contour);
                            transaction.AddNewlyCreatedDBObject(contour, true);
                        }
                    }
                    else
                    {
                        using (var contour = new Polyline3d(Poly3dType.SimplePoly, new Point3dCollection(vertices.ToArray()), closeContour))
                        {
                            contour.SetDatabaseDefaults(database);
                            contour.LayerId = polylineLayer;
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
