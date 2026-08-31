using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using OCR2Geometry.Models;

namespace OCR2Geometry.AutoCAD
{
    public static class PointCreator
    {
        public static void CreatePoints(IEnumerable<CoordinatePoint> points, double textHeight = 2.5, double textOffset = 2.5)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            var database = document.Database;

            using (document.LockDocument())
            using (var transaction = database.TransactionManager.StartTransaction())
            {
                var blockTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
                var modelSpace = (BlockTableRecord)transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                foreach (var point in points)
                {
                    var position = new Point3d(point.X, point.Y, 0.0);

                    var dbPoint = new DBPoint(position);
                    modelSpace.AppendEntity(dbPoint);
                    transaction.AddNewlyCreatedDBObject(dbPoint, true);

                    var label = new DBText
                    {
                        Position = new Point3d(point.X + textOffset, point.Y + textOffset, 0.0),
                        Height = textHeight,
                        TextString = point.Number.ToString()
                    };

                    modelSpace.AppendEntity(label);
                    transaction.AddNewlyCreatedDBObject(label, true);
                }

                transaction.Commit();
            }

            document.Editor.Regen();
        }
    }
}
