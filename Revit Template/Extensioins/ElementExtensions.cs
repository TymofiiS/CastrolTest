using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace RevitTemplate.Extensioins
{
    public static class ElementExtensions
    {
        public static CurveLoop FootprintOnLevel(
            this Element element, Level level)
        {
            // Init result
            CurveLoop res = new CurveLoop();

            // Check initial data
            if (element == null) return res;
            if (level == null) return res;


            // Init local variables
            Options opt = new Options();
            GeometryElement geomElem = element.get_Geometry(opt);
            if (geomElem == null) return res;

            double levelElevation = level.ProjectElevation;

            // Find end points of bottom adges using
            // a bottom object face for separating adges
            List<XYZ> tempPts = new List<XYZ>();
            foreach (GeometryObject geomObj in geomElem)
            {
                Solid geomSolid = geomObj as Solid;
                if (null == geomSolid) continue;

                foreach (Face geomFace in geomSolid.Faces)
                {
                    PlanarFace pf = geomFace as PlanarFace;

                    if (pf == null) continue;

                    if (!pf.FaceNormal.IsAlmostEqualTo(-XYZ.BasisZ))
                    { continue; }

                    EdgeArrayArray edgeLoops = pf.EdgeLoops;

                    foreach (EdgeArray edgeArray in edgeLoops)
                    {
                        foreach (Edge edge in edgeArray)
                        {
                            List<XYZ> points
                                = edge.Tessellate() as List<XYZ>;

                            tempPts.AddRange(points);
                        }
                    }
                }
            }
            if (tempPts.Count < 6) return res;

            // Due to all points are placed on face which are parallel
            // to level face, make paralel projection those points
            // just setting Z coordinate of the level elevation
            List<XYZ> translatedPts = tempPts
                .Select(x => new XYZ(x.X, x.Y, levelElevation))
                .ToList();

            // Create curve loop base on translated points
            for (int i = 0; i < translatedPts.Count; i = i + 2)
            {
                Line line = Line.CreateBound(
                    translatedPts[i], translatedPts[i + 1]);
                res.Append(line);
            }


            // Result
            return res;
        }

        public static double HeightBaseOnBoundingBox(this Element element)
        {
            if (element == null) return 0;

            BoundingBoxXYZ wallBb =
                element
                .get_BoundingBox(null);

            return wallBb == null ? 0 : wallBb.Max.Z - wallBb.Min.Z;
        }
    }
}
