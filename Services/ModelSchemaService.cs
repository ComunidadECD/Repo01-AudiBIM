using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace BIMQualityAuditor.Services
{
    public static class ModelSchemaService
    {
        public static List<string> GetAvailableCategories(Document doc)
        {
            var categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ALL_MODEL_ELEMENTS",
                "Walls",
                "Doors",
                "Windows",
                "Floors",
                "Ceilings",
                "Roofs",
                "Structural Framing",
                "Structural Columns",
                "Plumbing Fixtures",
                "Mechanical Equipment",
                "Lighting Fixtures"
            };

            if (doc != null)
            {
                try
                {
                    foreach (Category cat in doc.Settings.Categories)
                    {
                        if (cat != null && cat.CategoryType == CategoryType.Model && !cat.IsTagCategory)
                        {
                            if (!string.IsNullOrWhiteSpace(cat.Name))
                            {
                                categories.Add(cat.Name);
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback to default set
                }
            }

            return categories.OrderBy(c => c == "ALL_MODEL_ELEMENTS" ? 0 : 1).ThenBy(c => c).ToList();
        }

        public static List<string> GetParametersForCategory(Document doc, string categoryName)
        {
            var parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Type Name",
                "Level",
                "Código",
                "Comments",
                "Mark",
                "Length",
                "Area",
                "Volume",
                "Phase Created"
            };

            if (doc == null || string.IsNullOrWhiteSpace(categoryName))
            {
                return parameters.OrderBy(p => p).ToList();
            }

            try
            {
                var elements = ElementCollectorService.GetElementsForCategory(doc, categoryName);
                int sampleCount = 0;

                foreach (var elem in elements)
                {
                    if (elem == null) continue;

                    // Instance parameters
                    foreach (Parameter p in elem.Parameters)
                    {
                        if (p != null && p.Definition != null && !string.IsNullOrWhiteSpace(p.Definition.Name))
                        {
                            parameters.Add(p.Definition.Name);
                        }
                    }

                    // Type parameters
                    ElementId typeId = elem.GetTypeId();
                    if (typeId != null && typeId != ElementId.InvalidElementId)
                    {
                        Element typeElem = doc.GetElement(typeId);
                        if (typeElem != null)
                        {
                            foreach (Parameter p in typeElem.Parameters)
                            {
                                if (p != null && p.Definition != null && !string.IsNullOrWhiteSpace(p.Definition.Name))
                                {
                                    parameters.Add(p.Definition.Name);
                                }
                            }
                        }
                    }

                    sampleCount++;
                    if (sampleCount >= 15) break; // Sample up to 15 elements for efficiency
                }
            }
            catch
            {
                // Fallback to default parameters set
            }

            return parameters.OrderBy(p => p).ToList();
        }
    }
}
