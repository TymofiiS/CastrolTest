using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System.Collections.Generic;
using System.Linq;

namespace RevitTemplate.Extensioins
{
    public static class RoomExtensions
    {
        public static IList<Curve> ExtractRoomBorders(this Room room)
        {
            List<Curve> result = new List<Curve>();

            if (room == null) return result;

            IList<IList<BoundarySegment>> boundarySegments =
                room.GetBoundarySegments(new SpatialElementBoundaryOptions());

            foreach (IList<BoundarySegment> boundarySegment in boundarySegments)
            {
                if (!boundarySegment.Any()) continue;

                foreach (BoundarySegment boundary in boundarySegment)
                {
                    Curve curve = boundary.GetCurve();
                    if (curve?.Length == 0) continue;
                    result.Add(curve);
                }
            }

            return result;
        }
    }
}
