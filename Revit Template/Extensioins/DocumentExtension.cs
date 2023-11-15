using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace RevitTemplate.Extensioins
{
    public static class DocumentExtension
    {
        public static List<T> CollectElementsByType<T>(this Document doc) where T : class
        {
            FilteredElementCollector collector =
                new FilteredElementCollector(doc);
            List<T> result = collector
                .WhereElementIsNotElementType()
                .OfType<T>()
                .Cast<T>()
                .ToList();
            collector.Dispose();

            return result;
        }
    }
}
