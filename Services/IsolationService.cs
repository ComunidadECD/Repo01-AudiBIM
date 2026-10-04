using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace BIMQualityAuditor.Services
{
    public static class IsolationService
    {
        public static void IsolateElementsInView(Document doc, IEnumerable<long> elementIds)
        {
            if (doc == null || doc.ActiveView == null) return;

            View activeView = doc.ActiveView;
            var ids = elementIds.Select(id => new ElementId(id)).ToList();

            if (!ids.Any()) return;

            using (Transaction trans = new Transaction(doc, "AudiBIM - Aislar Elementos"))
            {
                trans.Start();
                activeView.IsolateElementsTemporary(ids);
                trans.Commit();
            }
        }

        public static void RestoreView(Document doc)
        {
            if (doc == null || doc.ActiveView == null) return;

            View activeView = doc.ActiveView;

            using (Transaction trans = new Transaction(doc, "AudiBIM - Restaurar Vista"))
            {
                trans.Start();
                if (activeView.IsTemporaryViewPropertiesModeEnabled())
                {
                    activeView.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                }
                else
                {
                    activeView.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                }
                trans.Commit();
            }
        }
    }
}
