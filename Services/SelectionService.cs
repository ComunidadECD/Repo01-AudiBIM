using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BIMQualityAuditor.Services
{
    public static class SelectionService
    {
        public static void SelectElements(UIDocument uidoc, IEnumerable<long> elementIds)
        {
            if (uidoc == null) return;

            var ids = elementIds.Select(id => new ElementId(id)).ToList();
            uidoc.Selection.SetElementIds(ids);
        }
    }
}
