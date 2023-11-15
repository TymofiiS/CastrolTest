using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using RevitTemplate.Extensioins;
using System;
using System.Collections.Generic;
using System.Linq;
using Document = Autodesk.Revit.DB.Document;
using Line = Autodesk.Revit.DB.Line;

namespace RevitTemplate
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class EntryCommand : IExternalCommand
    {
        const string WallTypeName = "FinishingType";

        public virtual Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIApplication app = commandData?.Application;
            UIDocument uiDoc = app?.ActiveUIDocument;
            Document doc = uiDoc?.Document;

            if (doc == null || !doc.IsValidObject) return Result.Failed;

            try
            {
                // Get all walls
                List<Wall> walls =
                    doc.CollectElementsByType<Wall>();
                if (!walls.Any()) return Result.Failed;

                // Get any wall height
                Wall wall = walls
                        .FirstOrDefault(x =>
                            x.get_BoundingBox(null) != null);
                if (wall == null) return Result.Failed;

                double wallHeight =
                    wall.HeightBaseOnBoundingBox();
                if (wallHeight == 0) return Result.Failed;

                // Get floor
                Floor floor =
                    doc.CollectElementsByType<Floor>()
                        .FirstOrDefault();
                if (floor == null) return Result.Failed;

                // Get floor level
                Level level = doc.GetElement(floor.LevelId) as Level;
                if (level == null) return Result.Failed;

                // Get floor border lines
                CurveLoop floorBorders = floor.FootprintOnLevel(level);
                if (!floorBorders.Any()) return Result.Failed;

                // Draw floor borders for control
                ModelLineCreator.DrawLines(
                    floorBorders.ToList(), doc.ActiveView);

                // For each wall find its center line
                // and move it to the nearest parallel floor border
                MoveWallsToNearestFloorBorder(
                    walls, floorBorders.ToList());

                // Create room object in floor center
                Room room = CreateRoomInFloorCenter(floor);
                if (room == null) return Result.Failed;

                // Get room border lines
                IList<Curve> borders = room.ExtractRoomBorders();
                if (!borders.Any()) return Result.Failed;

                // For each room border line create wall specific type
                // with founded height
                IList<Wall> finishingWalls =
                    CreateFinishingWalls(borders, wallHeight, level);
                if (!finishingWalls.Any()) return Result.Failed;

                // Find total area of created walls plus flour area
                double totalArea =
                    finishingWalls.Sum(x =>
                        x.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)
                        .AsDouble()) + room.Area;

                // Show to user total area
                TaskDialog.Show(
                    "Finishing calculator",
                    $"Total finishing area is {totalArea.SqFootToSqM()} m2.");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private void MoveWallsToNearestFloorBorder(
            List<Wall> walls, List<Curve> floorBorders)
        {
            foreach (Wall wall in walls)
            {
                // Wall location
                LocationCurve location =
                    wall.Location as LocationCurve;
                if (location == null) continue;

                // Wall location center point
                XYZ wallCenterLocationPnt =
                    (location.Curve.GetEndPoint(0) +
                    location.Curve.GetEndPoint(1)) / 2;

                // Wall location line 
                Line wallLocationLine =
                    Line.CreateBound(
                        location.Curve.GetEndPoint(0),
                        location.Curve.GetEndPoint(1));

                // Wall location line direction
                XYZ wallLocationLineDir =
                    wallLocationLine.Direction;

                // Get centers of floor borders parallel to wall location line
                List<XYZ> parallelBorderCenters = new List<XYZ>();
                foreach (Curve border in floorBorders)
                {
                    XYZ borderDirection = (border as Line).Direction;

                    if (borderDirection.IsAlmostEqualTo(wallLocationLineDir) ||
                        borderDirection.IsAlmostEqualTo(wallLocationLineDir.Negate()))
                    {
                        parallelBorderCenters.Add(
                            (border.GetEndPoint(0) + border.GetEndPoint(1)) / 2);
                    }
                }

                // Find from selected borders the nearest one
                // to wall location center point
                XYZ nearestCenter = null;
                double distance = double.MaxValue;
                foreach (XYZ borderCenter in parallelBorderCenters)
                {
                    // Case a wall does not need to be moved
                    if (wallCenterLocationPnt.IsAlmostEqualTo(borderCenter))
                    {
                        nearestCenter = borderCenter;
                        break;
                    }

                    // Wall is not in a proper position
                    double currentDistance =
                        wallCenterLocationPnt.DistanceTo(borderCenter);
                    if (currentDistance >= distance) continue;

                    nearestCenter = borderCenter;
                    distance = currentDistance;
                }

                // No need move the current wall
                if (wallCenterLocationPnt.IsAlmostEqualTo(nearestCenter))
                { continue; }

                // Move wall with transaction to the proper position
                Transaction tx = new Transaction(
                    wall.Document,
                    $"Move wall with id {wall.Id}");

                tx.Start();

                ElementTransformUtils.MoveElement(
                    wall.Document,
                    wall.Id,
                    nearestCenter - wallCenterLocationPnt);

                tx.Commit();

                tx.Dispose();
            }
        }

        private Room CreateRoomInFloorCenter(Floor floor)
        {
            Room res = null;

            // Combine UV floor center
            XYZ bbCenter = (
                floor.get_BoundingBox(null).Max +
                floor.get_BoundingBox(null).Min) / 2;
            UV roomLocation = new UV(bbCenter.X, bbCenter.Y);

            // Find floor level
            Level level =
                floor.Document.GetElement(floor.LevelId) as Level;

            // Create a new room with transaction
            Transaction tx = new Transaction(
                floor.Document,
                $"Create a room");

            tx.Start();

            res = floor.Document.Create.NewRoom(
                level, roomLocation);

            tx.Commit();

            tx.Dispose();

            // Result
            return res;
        }

        private IList<Wall> CreateFinishingWalls(
            IList<Curve> curves, double wallHeight, Level level)
        {
            IList<Wall> result = new List<Wall>();

            // Get wall type
            FilteredElementCollector collector =
                new FilteredElementCollector(level.Document);
            WallType wallType =
                collector.WhereElementIsElementType()
                .OfType<WallType>()
                .Cast<WallType>()
                .FirstOrDefault(x =>
                    string.Equals(x.Name, WallTypeName));

            collector.Dispose();

            if (wallType == null) return result;

            // Create wall for each curve with transaction
            Transaction tx = new Transaction(
                level.Document,
                $"Create finishing walls");

            tx.Start();

            foreach (Curve curve in curves)
            {
                Wall wall = Wall.Create(
                    level.Document,
                    curve,
                    wallType.Id,
                    level.Id,
                    wallHeight,
                    0,
                    false,
                    false);

                if (wall == null) continue;

                result.Add(wall);
            }

            tx.Commit();

            tx.Dispose();

            // Result
            return result;
        }
    }
}