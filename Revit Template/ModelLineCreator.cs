using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RevitTemplate
{
    public static class ModelLineCreator
    {
        public static void DrawLines(
            List<Curve> curves,
            View view)
        {
            // Check is need create transaction
            var createTransaction = !view.Document.IsModifiable;
            Transaction tx = null;

            if (createTransaction)
            {
                tx = new Transaction(view.Document);
                tx.Start("Create Model Curves");
            }

            Plane plane2 = Plane.CreateByNormalAndOrigin(
              view.ViewDirection,
              view.Origin);

            SketchPlane plane3 = SketchPlane.Create(
                view.Document, plane2);

            foreach (Curve c in curves)
            {
                view.Document.Create.NewModelCurve(c, plane3);
            }

            if (createTransaction)
            {
                tx.Commit();
                tx.Dispose();
            }
        }
    }
}
