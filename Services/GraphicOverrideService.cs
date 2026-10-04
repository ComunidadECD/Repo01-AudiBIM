using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace BIMQualityAuditor.Services
{
    public static class GraphicOverrideService
    {
        public static void HighlightElementsInView(Document doc, IEnumerable<long> elementIds, string hexColor)
        {
            if (doc == null || doc.ActiveView == null) return;

            View activeView = doc.ActiveView;
            var ids = elementIds.Select(id => new ElementId(id)).ToList();
            if (!ids.Any()) return;

            Color color = ParseHexColor(hexColor);
            ElementId fillPatternId = GetSolidFillPatternId(doc);

            using (Transaction trans = new Transaction(doc, "AudiBIM - Resaltar Colores"))
            {
                trans.Start();

                OverrideGraphicSettings ogs = new OverrideGraphicSettings();

                if (fillPatternId != ElementId.InvalidElementId)
                {
                    ogs.SetSurfaceForegroundPatternId(fillPatternId);
                    ogs.SetSurfaceForegroundPatternColor(color);
                    ogs.SetCutForegroundPatternId(fillPatternId);
                    ogs.SetCutForegroundPatternColor(color);
                }

                ogs.SetProjectionLineColor(color);

                foreach (var id in ids)
                {
                    try
                    {
                        activeView.SetElementOverrides(id, ogs);
                    }
                    catch
                    {
                        // Ignore individual override failures
                    }
                }

                trans.Commit();
            }
        }

        public static void ClearGraphicOverridesInView(Document doc, IEnumerable<long> elementIds)
        {
            if (doc == null || doc.ActiveView == null) return;

            View activeView = doc.ActiveView;
            var ids = elementIds.Select(id => new ElementId(id)).ToList();

            using (Transaction trans = new Transaction(doc, "AudiBIM - Limpiar Resaltado"))
            {
                trans.Start();
                OverrideGraphicSettings defaultOgs = new OverrideGraphicSettings();

                foreach (var id in ids)
                {
                    try
                    {
                        activeView.SetElementOverrides(id, defaultOgs);
                    }
                    catch
                    {
                        // Ignore
                    }
                }

                trans.Commit();
            }
        }

        private static Color ParseHexColor(string hex)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hex)) return new Color(229, 57, 53);
                hex = hex.TrimStart('#');
                if (hex.Length == 6)
                {
                    byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                    byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                    byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                    return new Color(r, g, b);
                }
            }
            catch { }

            return new Color(229, 57, 53);
        }

        private static ElementId GetSolidFillPatternId(Document doc)
        {
            FillPatternElement? solidPattern = new FilteredElementCollector(doc)
                .OfClass(typeof(FillPatternElement))
                .Cast<FillPatternElement>()
                .FirstOrDefault(fp => fp.GetFillPattern().IsSolidFill);

            return solidPattern != null ? solidPattern.Id : ElementId.InvalidElementId;
        }
    }
}
