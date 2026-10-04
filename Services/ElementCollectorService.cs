using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace BIMQualityAuditor.Services
{
    public static class ElementCollectorService
    {
        public static List<Element> GetElementsForCategory(Document doc, string categoryName)
        {
            if (doc == null) return new List<Element>();

            View activeView = doc.ActiveView;

            if (categoryName.Equals("ALL_MODEL_ELEMENTS", StringComparison.OrdinalIgnoreCase) ||
                categoryName.Equals("TODOS", StringComparison.OrdinalIgnoreCase))
            {
                return new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .WhereElementIsViewIndependent()
                    .Where(e => IsValidPhysicalBuildingElement(e, activeView))
                    .ToList();
            }

            BuiltInCategory? bic = MapToBuiltInCategory(categoryName);
            if (bic.HasValue)
            {
                return new FilteredElementCollector(doc)
                    .OfCategory(bic.Value)
                    .WhereElementIsNotElementType()
                    .WhereElementIsViewIndependent()
                    .Where(e => IsValidPhysicalBuildingElement(e, activeView))
                    .ToList();
            }

            // Fallback: search Category by Name
            foreach (Category cat in doc.Settings.Categories)
            {
                if (cat.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
                {
                    return new FilteredElementCollector(doc)
                        .OfCategoryId(cat.Id)
                        .WhereElementIsNotElementType()
                        .WhereElementIsViewIndependent()
                        .Where(e => IsValidPhysicalBuildingElement(e, activeView))
                        .ToList();
                }
            }

            return new List<Element>();
        }

        private static bool IsValidPhysicalBuildingElement(Element e, View activeView)
        {
            if (e == null || e.Category == null) return false;

            // 1. Category must be CategoryType.Model
            if (e.Category.CategoryType != CategoryType.Model) return false;

            // 2. Exclude Tags and Annotation subcategories
            if (e.Category.IsTagCategory) return false;

            long catId = e.Category.Id.Value;

            // 3. Exclude non-physical internal points, utility objects, legend components, project base points
            if (catId == (long)BuiltInCategory.OST_ProjectBasePoint ||
                catId == (long)BuiltInCategory.OST_SharedBasePoint ||
                e.Category.Name.Equals("Internal Origin", StringComparison.OrdinalIgnoreCase) ||
                e.Category.Name.Equals("Punto de origen interno", StringComparison.OrdinalIgnoreCase) ||
                catId == (long)BuiltInCategory.OST_LegendComponents ||
                catId == (long)BuiltInCategory.OST_Views ||
                catId == (long)BuiltInCategory.OST_Schedules ||
                catId == (long)BuiltInCategory.OST_Sheets ||
                catId == (long)BuiltInCategory.OST_Cameras ||
                catId == (long)BuiltInCategory.OST_SectionBox ||
                catId == (long)BuiltInCategory.OST_ProjectInformation ||
                catId == (long)BuiltInCategory.OST_Materials ||
                catId == (long)BuiltInCategory.OST_RvtLinks ||
                catId == (long)BuiltInCategory.OST_DesignOptions ||
                catId == (long)BuiltInCategory.OST_DesignOptionSets ||
                catId == (long)BuiltInCategory.OST_Phases ||
                catId == (long)BuiltInCategory.OST_CLines ||
                catId == (long)BuiltInCategory.OST_VolumeOfInterest)
            {
                return false;
            }

            // 4. Must have physical spatial presence (Location or BoundingBox)
            if (e.Location == null)
            {
                BoundingBoxXYZ bbox = e.get_BoundingBox(null);
                if (bbox == null) return false;
            }

            // 5. Must not be hidden in active view
            if (activeView != null && e.IsHidden(activeView))
            {
                return false;
            }

            return true;
        }

        private static BuiltInCategory? MapToBuiltInCategory(string name)
        {
            return name.Trim().ToLowerInvariant() switch
            {
                "walls" or "muros" => BuiltInCategory.OST_Walls,
                "doors" or "puertas" => BuiltInCategory.OST_Doors,
                "windows" or "ventanas" => BuiltInCategory.OST_Windows,
                "floors" or "suelos" or "pisos" => BuiltInCategory.OST_Floors,
                "roofs" or "cubiertas" or "techos" => BuiltInCategory.OST_Roofs,
                "structural columns" or "pilares estructurales" or "columnas" => BuiltInCategory.OST_StructuralColumns,
                "structural framing" or "armadura estructural" or "vigas" => BuiltInCategory.OST_StructuralFraming,
                "columns" or "pilares" => BuiltInCategory.OST_Columns,
                "rooms" or "habitaciones" => BuiltInCategory.OST_Rooms,
                "ceilings" or "falsos techos" => BuiltInCategory.OST_Ceilings,
                "stairs" or "escaleras" => BuiltInCategory.OST_Stairs,
                "railing" or "railings" or "barrandillas" => BuiltInCategory.OST_StairsRailing,
                _ => null
            };
        }
    }
}
